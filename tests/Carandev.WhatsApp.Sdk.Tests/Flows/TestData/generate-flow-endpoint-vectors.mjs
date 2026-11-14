// Generates flow-endpoint-vectors.json, the independent reference for FlowEndpointCryptoTests.
// It mirrors what the WhatsApp client and Meta's Node.js endpoint example do (OpenSSL via node:crypto).
// The RSA key is created for tests only and protects nothing.
// Regenerate: node generate-flow-endpoint-vectors.mjs > flow-endpoint-vectors.json
import crypto from "node:crypto";
// RSA key pair, as a business would generate for its Flow endpoint.
const { publicKey, privateKey } = crypto.generateKeyPairSync("rsa", { modulusLength: 2048 });
const passphrase = "test-passphrase";
const pkcs8 = privateKey.export({ type: "pkcs8", format: "pem" });
const pkcs8Encrypted = privateKey.export({ type: "pkcs8", format: "pem", cipher: "aes-256-cbc", passphrase });
const pkcs1Legacy = privateKey.export({ type: "pkcs1", format: "pem", cipher: "aes-256-cbc", passphrase });

// What the WhatsApp client does: random AES-128 key and 16-byte IV.
const aesKey = crypto.randomBytes(16);
const iv = crypto.randomBytes(16);
const payload = JSON.stringify({ version: "3.0", action: "data_exchange", screen: "APPOINTMENT", data: { department: "beauty", guests: 2 }, flow_token: "booking-42" });
const cipher = crypto.createCipheriv("aes-128-gcm", aesKey, iv);
const encryptedFlowData = Buffer.concat([cipher.update(payload, "utf8"), cipher.final(), cipher.getAuthTag()]).toString("base64");
const encryptedAesKey = crypto.publicEncrypt({ key: publicKey, padding: crypto.constants.RSA_PKCS1_OAEP_PADDING, oaepHash: "sha256" }, aesKey).toString("base64");

// Meta's encryptResponse (examples/endpoint/nodejs/basic/src/encryption.js).
function encryptResponse(response) {
  const flipped = []; for (const pair of iv.entries()) flipped.push(~pair[1]);
  const c = crypto.createCipheriv("aes-128-gcm", aesKey, Buffer.from(flipped));
  return Buffer.concat([c.update(response, "utf8"), c.final(), c.getAuthTag()]).toString("base64");
}
const responseJson = JSON.stringify({ screen: "SUCCESS", data: { extension_message_response: { params: { flow_token: "booking-42" } } } });

// Generic GCM vector with a 60-byte IV and AAD (non-96-bit IV path), cross-checks GHASH.
const gk = Buffer.from("feffe9928665731c6d6a8f9467308308", "hex");
const giv = Buffer.from("9313225df88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b39", "hex");
const gp = Buffer.from("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b39", "hex");
const ga = Buffer.from("feedfacedeadbeeffeedfacedeadbeefabaddad2", "hex");
const gc = crypto.createCipheriv("aes-128-gcm", gk, giv); gc.setAAD(ga);
const gct = Buffer.concat([gc.update(gp), gc.final()]);

console.log(JSON.stringify({
  privateKeyPkcs8: pkcs8, privateKeyPkcs8Encrypted: pkcs8Encrypted, privateKeyPkcs1Legacy: pkcs1Legacy, passphrase,
  request: { encrypted_flow_data: encryptedFlowData, encrypted_aes_key: encryptedAesKey, initial_vector: iv.toString("base64") },
  decryptedPayload: payload, responseJson, encryptedResponse: encryptResponse(responseJson),
  gcm: { key: gk.toString("hex"), iv: giv.toString("hex"), aad: ga.toString("hex"), plaintext: gp.toString("hex"), ciphertext: gct.toString("hex"), tag: gc.getAuthTag().toString("hex") },
}, null, 2));
