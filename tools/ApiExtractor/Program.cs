using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Carandev.WhatsApp.Sdk;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ApiExtractor <output.json>");
    return 1;
}

var assembly = typeof(WhatsAppOptions).Assembly;
var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
if (!File.Exists(xmlPath))
{
    Console.Error.WriteLine($"XML documentation not found at {xmlPath}.");
    return 1;
}

var docs = XDocument.Load(xmlPath).Descendants("member")
    .ToDictionary(m => (string)m.Attribute("name")!, m => m);

var nullability = new NullabilityInfoContext();
var types = assembly.GetExportedTypes()
    .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))
    .OrderBy(t => t.FullName, StringComparer.Ordinal)
    .Select(DescribeType)
    .ToList();

var namespaces = types
    .GroupBy(t => t.Namespace)
    .OrderBy(g => g.Key, StringComparer.Ordinal)
    .Select(g => new { name = g.Key, types = g.ToList() });

var output = new { assembly = assembly.GetName().Name, namespaces };
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
File.WriteAllText(args[0], JsonSerializer.Serialize(output, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}));
Console.WriteLine($"Wrote {types.Count} types to {args[0]}");
return 0;

TypeDoc DescribeType(Type type)
{
    var doc = Doc(TypeId(type));
    if (IsInherit(doc))
    {
        doc = type.GetInterfaces().Select(i => Doc(TypeId(i))).FirstOrDefault(d => d is not null && !IsInherit(d))
            ?? (type.BaseType is { } bt ? Doc(TypeId(bt)) : null);
    }

    var members = new List<MemberDoc>();
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    if (type.IsEnum)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            members.Add(Member("field", field.Name, field.Name, Doc($"F:{TypeId(type)[2..]}.{field.Name}")));
        }
    }
    else
    {
        foreach (var ctor in type.GetConstructors(flags).Where(c => !c.IsStatic))
        {
            var ctorDoc = Doc(MethodId(ctor));
            // Skip compiler-generated parameterless constructors, which carry no documentation.
            if (ctorDoc is null && ctor.GetParameters().Length == 0) continue;
            members.Add(Member("constructor", Simple(type), $"{Simple(type)}({Params(ctor)})", ctorDoc));
        }

        foreach (var property in type.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0))
        {
            var accessors = property.SetMethod is { IsPublic: true } setter
                ? (setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit") ? "{ get; init; }" : "{ get; set; }")
                : "{ get; }";
            var isStatic = property.GetMethod?.IsStatic == true ? "static " : string.Empty;
            members.Add(Member("property", property.Name, $"{isStatic}{Name(property.PropertyType, nullability.Create(property))} {property.Name} {accessors}", MemberDocFor(property.GetMethod ?? property.SetMethod!, $"P:{TypeId(type)[2..]}.{property.Name}", m => $"P:{TypeId(m.DeclaringType!)[2..]}.{property.Name}")));
        }

        foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName && !IsRecordPlumbing(type, m)))
        {
            var methodDoc = MemberDocFor(method, MethodId(method), MethodId);
            // Skip undocumented overrides of System.Object members (record/compiler plumbing).
            if (methodDoc is null && method.Name is "ToString" or "Equals" or "GetHashCode") continue;
            var isStatic = method.IsStatic ? "static " : string.Empty;
            var generic = method.IsGenericMethod ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>" : string.Empty;
            var extension = method.IsDefined(typeof(ExtensionAttribute)) ? "this " : string.Empty;
            var returns = Name(method.ReturnType, nullability.Create(method.ReturnParameter));
            members.Add(Member("method", method.Name, $"{isStatic}{returns} {method.Name}{generic}({extension}{Params(method)})", methodDoc));
        }

        foreach (var field in type.GetFields(flags).Where(f => f.IsPublic))
        {
            var modifier = field.IsLiteral ? "const " : field.IsStatic ? "static readonly " : string.Empty;
            members.Add(Member("field", field.Name, $"{modifier}{Name(field.FieldType, nullability.Create(field))} {field.Name}", Doc($"F:{TypeId(type)[2..]}.{field.Name}")));
        }
    }

    return new TypeDoc(
        type.Namespace ?? string.Empty,
        Simple(type),
        Slug(type),
        Kind(type),
        Declaration(type),
        Text(doc?.Element("summary")),
        Text(doc?.Element("remarks")),
        members);
}

MemberDoc Member(string kind, string name, string signature, XElement? doc) => new(
    kind,
    name,
    signature,
    Text(doc?.Element("summary")),
    Text(doc?.Element("remarks")),
    Text(doc?.Element("returns")),
    doc?.Elements("param").Select(p => new ParamDoc((string)p.Attribute("name")!, Text(p))).ToList() ?? [],
    doc?.Elements("exception").Select(e => new ParamDoc(CrefName((string?)e.Attribute("cref")), Text(e))).ToList() ?? []);

XElement? Doc(string id) => docs.TryGetValue(id, out var element) ? element : null;

static bool IsInherit(XElement? doc) => doc is null || doc.Element("inheritdoc") is not null && doc.Element("summary") is null;

// Resolves <inheritdoc/> by walking to the implemented interface member, then the overridden base member.
XElement? MemberDocFor(MethodInfo accessor, string ownId, Func<MethodInfo, string> idOf)
{
    var own = Doc(ownId);
    if (!IsInherit(own)) return own;
    var type = accessor.DeclaringType!;
    foreach (var iface in type.GetInterfaces())
    {
        var map = type.GetInterfaceMap(iface);
        var index = Array.IndexOf(map.TargetMethods, accessor);
        if (index < 0) continue;
        var target = map.InterfaceMethods[index];
        var candidate = accessor.IsSpecialName
            ? Doc(ReplaceDeclaring(ownId, type, target.DeclaringType!))
            : Doc(idOf(target));
        if (!IsInherit(candidate)) return candidate;
    }

    var baseDefinition = accessor.GetBaseDefinition();
    if (baseDefinition != accessor && baseDefinition.DeclaringType != typeof(object))
    {
        var candidate = accessor.IsSpecialName
            ? Doc(ReplaceDeclaring(ownId, type, baseDefinition.DeclaringType!))
            : Doc(idOf(baseDefinition));
        if (!IsInherit(candidate)) return candidate;
    }

    return own;
}

static string ReplaceDeclaring(string id, Type from, Type to) =>
    id[..2] + DocTypeName(to, false) + id[(2 + DocTypeName(from, false).Length)..];

static bool IsRecordPlumbing(Type type, MethodInfo method) =>
    method.Name is "<Clone>$" or "PrintMembers" or "Deconstruct" ||
    (method.Name is "Equals" or "GetHashCode" or "ToString" && type.GetMethod("<Clone>$") is not null) ||
    (method.Name == "Equals" && method.GetParameters() is [{ ParameterType: var p }] && p == type);

static string Kind(Type type) =>
    type.IsEnum ? "enum"
    : type.IsInterface ? "interface"
    : type.IsValueType ? (type.GetMethod("<Clone>$") is not null ? "record struct" : "struct")
    : type.IsSubclassOf(typeof(Delegate)) ? "delegate"
    : type.GetMethod("<Clone>$") is not null ? "record"
    : "class";

static string Declaration(Type type)
{
    var modifiers = type.IsInterface || type.IsEnum || type.IsValueType ? string.Empty
        : type.IsAbstract && type.IsSealed ? "static "
        : type.IsAbstract ? "abstract "
        : type.IsSealed ? "sealed "
        : string.Empty;
    var bases = new List<string>();
    if (type.BaseType is { } baseType && baseType != typeof(object) && baseType != typeof(ValueType) && baseType != typeof(Enum))
    {
        bases.Add(Name(baseType, null));
    }

    var inherited = type.BaseType?.GetInterfaces() ?? [];
    bases.AddRange(type.GetInterfaces()
        .Where(i => !inherited.Contains(i) && i.IsPublic && !(i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)))
        .Select(i => Name(i, null)));
    var suffix = bases.Count > 0 ? " : " + string.Join(", ", bases) : string.Empty;
    return $"public {modifiers}{Kind(type)} {Simple(type)}{suffix}";
}

string Params(MethodBase method) => string.Join(", ", method.GetParameters().Select(p =>
{
    var type = Name(p.ParameterType, nullability.Create(p));
    var value = !p.HasDefaultValue ? string.Empty
        : p.DefaultValue is null && p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) is null ? " = default"
        : " = " + Literal(p.DefaultValue);
    return $"{type} {p.Name}{value}";
}));

static string Literal(object? value) => value switch
{
    null => "null",
    string s => $"\"{s}\"",
    bool b => b ? "true" : "false",
    _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "default",
};

static string Name(Type type, NullabilityInfo? info)
{
    if (Nullable.GetUnderlyingType(type) is { } underlying)
    {
        return Name(underlying, null) + "?";
    }

    if (type.IsByRef)
    {
        return "ref " + Name(type.GetElementType()!, info);
    }

    var nullable = info is { ReadState: NullabilityState.Nullable } && !type.IsValueType ? "?" : string.Empty;
    if (type.IsArray)
    {
        return Name(type.GetElementType()!, info?.ElementType) + "[]" + nullable;
    }

    var keyword = type.IsEnum ? null : Type.GetTypeCode(type) switch
    {
        TypeCode.String => "string",
        TypeCode.Boolean => "bool",
        TypeCode.Int32 => "int",
        TypeCode.Int64 => "long",
        TypeCode.Double => "double",
        TypeCode.Decimal => "decimal",
        TypeCode.Byte => "byte",
        _ => type == typeof(object) ? "object" : type == typeof(void) ? "void" : null,
    };
    if (keyword is not null)
    {
        return keyword + nullable;
    }

    if (type.IsGenericType)
    {
        var args = type.GetGenericArguments();
        var argInfos = info?.GenericTypeArguments;
        var rendered = args.Select((a, i) => Name(a, argInfos is { Length: > 0 } && i < argInfos.Length ? argInfos[i] : null));
        return $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", rendered)}>{nullable}";
    }

    return type.Name + nullable;
}

static string Simple(Type type) => Name(type, null);

static string Slug(Type type) => (type.FullName ?? type.Name).Replace('+', '.').Replace("`", "-").ToLowerInvariant();

static string TypeId(Type type) => "T:" + DocTypeName(type, false);

static string DocTypeName(Type type, bool parameter)
{
    if (type.IsGenericParameter)
    {
        return (type.DeclaringMethod is null ? "`" : "``") + type.GenericParameterPosition;
    }

    if (type.IsByRef) return DocTypeName(type.GetElementType()!, parameter) + "@";
    if (type.IsArray) return DocTypeName(type.GetElementType()!, parameter) + "[]";

    if (parameter && type.IsGenericType && !type.IsGenericTypeDefinition)
    {
        var definition = type.GetGenericTypeDefinition();
        var baseName = (definition.FullName ?? definition.Name).Replace('+', '.');
        baseName = baseName[..baseName.IndexOf('`')];
        return $"{baseName}{{{string.Join(",", type.GetGenericArguments().Select(a => DocTypeName(a, true)))}}}";
    }

    return (type.FullName ?? $"{type.Namespace}.{type.Name}").Replace('+', '.');
}

static string MethodId(MethodBase method)
{
    var name = method.IsConstructor ? "#ctor" : method.Name;
    if (method is MethodInfo { IsGenericMethod: true } generic)
    {
        name += "``" + generic.GetGenericArguments().Length;
    }

    var parameters = method.GetParameters();
    var signature = parameters.Length == 0 ? string.Empty : $"({string.Join(",", parameters.Select(p => DocTypeName(p.ParameterType, true)))})";
    return $"M:{DocTypeName(method.DeclaringType!, false)}.{name}{signature}";
}

static string CrefName(string? cref)
{
    if (string.IsNullOrEmpty(cref)) return string.Empty;
    var id = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
    var paren = id.IndexOf('(');
    if (paren >= 0) id = id[..paren];
    var parts = id.Split('.');
    var last = parts[^1] == "#ctor" ? parts[^2] : parts[^1];
    return Regex.Replace(last, "`+\\d+", string.Empty);
}

// Converts documentation XML to a small, safe HTML subset.
static string? Text(XElement? element)
{
    if (element is null) return null;
    var builder = new StringBuilder();
    foreach (var node in element.Nodes())
    {
        Render(node, builder);
    }

    var html = Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
    return html.Length == 0 ? null : html;
}

static void Render(XNode node, StringBuilder builder)
{
    switch (node)
    {
        case XText text:
            builder.Append(System.Net.WebUtility.HtmlEncode(text.Value));
            break;
        case XElement e when e.Name == "see" && e.Attribute("langword") is { } word:
            builder.Append($"<code>{System.Net.WebUtility.HtmlEncode(word.Value)}</code>");
            break;
        case XElement e when e.Name is { LocalName: "see" or "seealso" }:
            var cref = (string?)e.Attribute("cref");
            var label = e.Nodes().Any() ? Text(e) : System.Net.WebUtility.HtmlEncode(CrefName(cref));
            builder.Append(cref is not null && cref.StartsWith("T:Carandev.", StringComparison.Ordinal)
                ? $"<a data-type=\"{System.Net.WebUtility.HtmlEncode(cref[2..].Replace("`", "-").ToLowerInvariant())}\"><code>{label}</code></a>"
                : $"<code>{label}</code>");
            break;
        case XElement e when e.Name is { LocalName: "paramref" or "typeparamref" }:
            builder.Append($"<code>{System.Net.WebUtility.HtmlEncode((string?)e.Attribute("name") ?? string.Empty)}</code>");
            break;
        case XElement e when e.Name == "c":
            builder.Append($"<code>{System.Net.WebUtility.HtmlEncode(e.Value)}</code>");
            break;
        case XElement e when e.Name == "code":
            builder.Append($"<pre><code>{System.Net.WebUtility.HtmlEncode(e.Value.Trim())}</code></pre>");
            break;
        case XElement e when e.Name == "para":
            builder.Append("<p>");
            foreach (var child in e.Nodes()) Render(child, builder);
            builder.Append("</p>");
            break;
        case XElement e when e.Name == "list":
            builder.Append("<ul>");
            foreach (var item in e.Elements("item"))
            {
                builder.Append("<li>");
                foreach (var child in (item.Element("description") ?? item).Nodes()) Render(child, builder);
                builder.Append("</li>");
            }

            builder.Append("</ul>");
            break;
        case XElement e:
            foreach (var child in e.Nodes()) Render(child, builder);
            break;
    }
}

record TypeDoc(string Namespace, string Name, string Slug, string Kind, string Declaration, string? Summary, string? Remarks, List<MemberDoc> Members);

record MemberDoc(string Kind, string Name, string Signature, string? Summary, string? Remarks, string? Returns, List<ParamDoc> Params, List<ParamDoc> Exceptions);

record ParamDoc(string Name, string? Text);
