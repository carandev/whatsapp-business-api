# Changelog

## [0.2.1](https://github.com/carandev/whatsapp-business-api/compare/v0.2.0...v0.2.1) (2026-11-05)


### Bug Fixes

* **resilience:** support MaxRetries = 0 and reject negative values ([804e6c4](https://github.com/carandev/whatsapp-business-api/commit/804e6c4d4eb74475ccaa35fa72a61f5e8a6b93b1)), closes [#56](https://github.com/carandev/whatsapp-business-api/issues/56)
* **templates:** deserialize HEADER components by format ([1c78f2e](https://github.com/carandev/whatsapp-business-api/commit/1c78f2e28b46ec4d6b73521b90d42a4b4f3679e8)), closes [#57](https://github.com/carandev/whatsapp-business-api/issues/57)
* **templates:** require lowercase named placeholders; uppercase names now throw ([51db721](https://github.com/carandev/whatsapp-business-api/commit/51db721a1df71d30c8c0d18db28c0b58b4249e9c)), closes [#58](https://github.com/carandev/whatsapp-business-api/issues/58)

## [0.2.0](https://github.com/carandev/whatsapp-business-api/compare/v0.1.0...v0.2.0) (2026-11-05)


### Features

* **media:** add resumable upload for template media header handles ([89f8ee4](https://github.com/carandev/whatsapp-business-api/commit/89f8ee4420455bf1fd7cfceebf1951932f94404c)), closes [#52](https://github.com/carandev/whatsapp-business-api/issues/52)
* **media:** resume a failed template media upload from the reported offset ([debbc61](https://github.com/carandev/whatsapp-business-api/commit/debbc61acba9929ae9d634533ab1ddafc538c0bf)), closes [#52](https://github.com/carandev/whatsapp-business-api/issues/52)
* **phone-numbers:** add cursor paging to ListPhoneNumbersAsync ([ac0afaf](https://github.com/carandev/whatsapp-business-api/commit/ac0afaf0780cd037c560c554275ccb4ed1fbd8ce)), closes [#53](https://github.com/carandev/whatsapp-business-api/issues/53)
* SDK 0.2.0 — webhook context, dependency fix, timeout, templates, media, paging ([cd6b8ae](https://github.com/carandev/whatsapp-business-api/commit/cd6b8ae5cc0c9ab620efaecc78da79f2c4d57b8d))
* **templates:** add examples for body, text header and URL button variables ([b9caf54](https://github.com/carandev/whatsapp-business-api/commit/b9caf54dee69dfdc1d586afa02c41c823ea956f9)), closes [#50](https://github.com/carandev/whatsapp-business-api/issues/50)
* **templates:** add parameter_format to CreateTemplateRequest ([3ad54cf](https://github.com/carandev/whatsapp-business-api/commit/3ad54cf19c13fcb76596d4fa669d570107c17d5d)), closes [#51](https://github.com/carandev/whatsapp-business-api/issues/51)
* **webhooks:** add batch handlers for per-change dispatch ([17a8dab](https://github.com/carandev/whatsapp-business-api/commit/17a8dab8378ec22a6918abbc13bf4c34a77be8d5)), closes [#55](https://github.com/carandev/whatsapp-business-api/issues/55)
* **webhooks:** pass WebhookContext with receiving number and sender profile to handlers ([0826c8e](https://github.com/carandev/whatsapp-business-api/commit/0826c8efd52c695973d9c9588581daad7998f296)), closes [#45](https://github.com/carandev/whatsapp-business-api/issues/45)


### Bug Fixes

* **deps:** raise Microsoft.Extensions.* floor to 10.0.12 ([f73e540](https://github.com/carandev/whatsapp-business-api/commit/f73e5405713c33e5f2a544613555b6c2fa6df5fd)), closes [#46](https://github.com/carandev/whatsapp-business-api/issues/46)
* **messages:** make ListRow description optional ([ee66ce0](https://github.com/carandev/whatsapp-business-api/commit/ee66ce0716f9b140070545bdf0c097dc21d65d97)), closes [#48](https://github.com/carandev/whatsapp-business-api/issues/48)
* **resilience:** enforce WhatsAppOptions.Timeout per attempt ([9674b1f](https://github.com/carandev/whatsapp-business-api/commit/9674b1fa849b1c8bec41b62e1d3eda3897796436)), closes [#49](https://github.com/carandev/whatsapp-business-api/issues/49)
* **templates:** serialize media header example as header_handle ([bcdcd17](https://github.com/carandev/whatsapp-business-api/commit/bcdcd1785c40500f327347c22957b9b22e91d559)), closes [#47](https://github.com/carandev/whatsapp-business-api/issues/47)
* **webhooks:** let OnErrorAsync exceptions escape without a second OnErrorAsync call ([ad3e8d1](https://github.com/carandev/whatsapp-business-api/commit/ad3e8d11677fdc2d510022373d10935c639c6afd))

## 0.1.0 (2026-11-04)


### ⚠ BREAKING CHANGES

* root namespace WhatsAppCloudApi is now Carandev.WhatsApp.Sdk and WhatsAppCloudApi.Extensions.DependencyInjection is now Carandev.WhatsApp.Sdk.DependencyInjection. Nothing was published yet.

### Features

* **business-profile:** implement WhatsAppBusinessProfileClient methods ([82c9889](https://github.com/carandev/whatsapp-business-api/commit/82c9889eaf2ba1d0d8ff2b184d09e5e1a1c06fca))
* **client:** add CreateAbsoluteRequest for absolute-URI requests ([afe9f97](https://github.com/carandev/whatsapp-business-api/commit/afe9f97082f88962fc58ab2574832509589f267b))
* **client:** add typed HTTP client, JSON source-gen serialization, and log redaction ([12d3ea4](https://github.com/carandev/whatsapp-business-api/commit/12d3ea4f49dcf262347d93661024690a427a3fbb))
* **clients:** add stub interfaces for the 6 WhatsApp Cloud API resource clients ([2ba082f](https://github.com/carandev/whatsapp-business-api/commit/2ba082f2a7dc5d795b66069f1a19278de04492e5))
* **di:** add AddWhatsAppCloudApi dependency injection extension ([2bb9611](https://github.com/carandev/whatsapp-business-api/commit/2bb9611a1f2a64bdda1a852c2c54d22d465f202c))
* **errors:** add transient error classifier and Meta user-facing fields ([9a1af7c](https://github.com/carandev/whatsapp-business-api/commit/9a1af7c0cf3f3c9e6552b79a130eeb6a60bd723c))
* **errors:** add WhatsAppException hierarchy and factory ([04af15d](https://github.com/carandev/whatsapp-business-api/commit/04af15d58ed492650796785fd45e4c2a89daab23))
* **media:** add MediaSizeExceededException for pre-flight upload validation ([9979583](https://github.com/carandev/whatsapp-business-api/commit/99795838ced212f89a2d8bcda16f1070fcf3bebd))
* **media:** add MediaUploadResponse DTO and register in JSON serializer context ([a247d97](https://github.com/carandev/whatsapp-business-api/commit/a247d97d77c85b541dcd0ad729cf78a919c94f7e))
* **media:** add MediaUrlResponse DTO and register in JSON serializer context ([a8249e2](https://github.com/carandev/whatsapp-business-api/commit/a8249e2edd0864b279159026c7e9718c1d639dee))
* **media:** implement WhatsAppMediaClient.GetMediaUrlAsync, DownloadAsync, DownloadBytesAsync ([6cdc593](https://github.com/carandev/whatsapp-business-api/commit/6cdc59396d48f2a7a91051de7be038add3e965bf))
* **media:** implement WhatsAppMediaClient.UploadAsync overloads ([d5362f3](https://github.com/carandev/whatsapp-business-api/commit/d5362f3392c2c92610f31fe2e72e5b70bc89c193))
* **messages:** add interactive header hierarchy and reply-buttons models ([0293ba9](https://github.com/carandev/whatsapp-business-api/commit/0293ba90eece604bb65d40df69819f8d55b5f843))
* **messages:** add list and CTA URL interactive message models ([eed6a9a](https://github.com/carandev/whatsapp-business-api/commit/eed6a9ae5f10055068b4feb18ebceb3fdc8d73a9))
* **messages:** add per-message-kind send requests and response envelope ([050258a](https://github.com/carandev/whatsapp-business-api/commit/050258ac71d4fb9ec6a22848322f0e1acb2a5240))
* **messages:** add send-message foundation models ([705c21c](https://github.com/carandev/whatsapp-business-api/commit/705c21c25832bec52434e9df93fb158a1843ae75))
* **messages:** add send-time template parameter hierarchy ([b8faffe](https://github.com/carandev/whatsapp-business-api/commit/b8faffee12343642aca41fb8bef72934585c3822))
* **messages:** implement WhatsAppMessagesClient list and CTA URL send methods ([c4c09d5](https://github.com/carandev/whatsapp-business-api/commit/c4c09d55024da22984739cce510668d54ebd2ff2))
* **messages:** implement WhatsAppMessagesClient send methods ([7a77aee](https://github.com/carandev/whatsapp-business-api/commit/7a77aee41310a9a89a63fc5c96497d2ed6513f72))
* **messages:** implement WhatsAppMessagesClient.SendReplyButtonsAsync ([4053952](https://github.com/carandev/whatsapp-business-api/commit/40539525bbed8c338582143fa4564d7f80941b32))
* **observability:** log request/response summary and redacted bodies in WhatsAppClient ([3db1b0c](https://github.com/carandev/whatsapp-business-api/commit/3db1b0c8da66ebd467aaeb7e9ad238652ded01a1))
* **options:** add WhatsAppOptions configuration binding and validation ([e50398d](https://github.com/carandev/whatsapp-business-api/commit/e50398d8d6ab4037e773e68cf7f0cefb230dbb49))
* **phone-numbers:** add phone number and business profile DTOs ([e5747c1](https://github.com/carandev/whatsapp-business-api/commit/e5747c1455f04ec7433ba851f2255cfae5a9df0f))
* **phone-numbers:** implement WhatsAppPhoneNumbersClient methods ([41e0bef](https://github.com/carandev/whatsapp-business-api/commit/41e0bef6923872474b435f1fec4df26661378472))
* **resilience:** throw WhatsAppException on retry exhaustion ([af62986](https://github.com/carandev/whatsapp-business-api/commit/af62986f0d061ab6872432b0c1a6422d9a149272))
* **resilience:** wire retry pipeline with GraphErrorPeek and transient classification ([c6d34a1](https://github.com/carandev/whatsapp-business-api/commit/c6d34a1c4cf10811d9c67764476f01e376077272))
* **sample:** add console sample demonstrating DI registration ([c70cc4e](https://github.com/carandev/whatsapp-business-api/commit/c70cc4ed893f9aaea9d021a6b122736ff74c5b77))
* **samples:** replace DI-only sample with SendHelloWorld end-to-end demo ([265536d](https://github.com/carandev/whatsapp-business-api/commit/265536d56094e8a77169f5fc1018a92caa96eea0))
* **sdk:** add message template component model hierarchy ([7a03ed5](https://github.com/carandev/whatsapp-business-api/commit/7a03ed5820e5e6c6107c71a023a310a40dd1c579))
* **sdk:** add message template request/response DTOs and serializer registration ([d94fd07](https://github.com/carandev/whatsapp-business-api/commit/d94fd079ba135b30b15856f1a2933e2753ec1306))
* **sdk:** add Result contract and WABA request seam ([158b0b5](https://github.com/carandev/whatsapp-business-api/commit/158b0b5246843ff05c5af1350577a585b2fd4e72))
* **sdk:** implement WhatsAppTemplatesClient CRUD operations ([e3d0772](https://github.com/carandev/whatsapp-business-api/commit/e3d077217f21ce47d5c8bdb7f61175f91031b29d))
* single Carandev.WhatsApp.Sdk package, CI/CD to NuGet, and documentation site ([6a8557a](https://github.com/carandev/whatsapp-business-api/commit/6a8557a8fa4c13c8239f76608d99ebb349892a48))
* **webhooks:** add AppSecret and WebhookVerifyToken to WhatsAppOptions ([7fd6403](https://github.com/carandev/whatsapp-business-api/commit/7fd6403fb8ba10629f30ea78917e80a3e407450d))
* **webhooks:** add inbound message payload models for all 13 kinds ([9e1c730](https://github.com/carandev/whatsapp-business-api/commit/9e1c730db6754355605c9dabe1940cba559f2b18))
* **webhooks:** add inbound status payload models ([24cb03f](https://github.com/carandev/whatsapp-business-api/commit/24cb03f969cdd8bbae5f2ff317972ae9628d228f))
* **webhooks:** add UseWhatsAppWebhooks middleware for GET handshake and POST dispatch ([62fdd17](https://github.com/carandev/whatsapp-business-api/commit/62fdd1770e063e1d97899b89289f894f1c6a92d7))
* **webhooks:** add WhatsAppSignatureValidator for HMAC signature verification ([e9de4e7](https://github.com/carandev/whatsapp-business-api/commit/e9de4e7dab9ed8089958897d5bdc5e1d5ec9393b))
* **webhooks:** scaffold Extensions.AspNetCore project with handlers base class ([7645014](https://github.com/carandev/whatsapp-business-api/commit/7645014da7cdf8d3741d05aeeda748d2b56712e8))
* WhatsApp Cloud API SDK — complete MVP milestone (issues [#1](https://github.com/carandev/whatsapp-business-api/issues/1)-[#8](https://github.com/carandev/whatsapp-business-api/issues/8)) ([148f393](https://github.com/carandev/whatsapp-business-api/commit/148f393662e2a7608831cce821e6d44c98bd80bf))


### Code Refactoring

* rename package, assembly, and namespaces to Carandev.WhatsApp.Sdk ([47ddea1](https://github.com/carandev/whatsapp-business-api/commit/47ddea1d829e6540964d1f0682b5126a04a28543))
