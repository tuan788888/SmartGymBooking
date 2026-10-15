# AI Virtual PT chat

Customer navigation opens `/AIChat`. Conversations and messages use the existing scaffolded Aiconversation/Aimessage models and SQL tables; no schema changes are required. GET selects the latest owned conversation unless `newChat=true`. Creating a new chat shows an empty screen without inserting a record. The first valid question saves a conversation and USER message together. Replies are saved as ASSISTANT messages. Provider failures keep the user's question and show a generic error.

## Configuration

The committed default is `AI:Provider=DEMO`, with `AI:FallbackToDemo=true`. DEMO uses local keyword responses and never sends an OpenAI request. It supports greetings, BMI, current workout, goals, muscle gain, weight loss, chest/back/leg training and recovery. It is a demonstration responder, not a generative model.

To enable real Responses API requests, set process/deployment environment variables and restart:

```text
AI__Provider=OPENAI
OPENAI_API_KEY=<your API key>
AI__OpenAI__Model=gpt-6-luna
AI__OpenAI__MaxOutputTokens=500
AI__FallbackToDemo=true
```

Do not commit API keys. OPENAI requests incur API usage and include the customer's profile, latest workout and up to ten previous messages with explicit user/assistant roles. The current question is appended once. Requests set `store=false`, use a 45-second HTTP timeout and collect all output_text parts (or refusal text). Missing keys, HTTP errors, timeouts, malformed/empty responses use clearly labelled demo fallback when enabled; disabling fallback shows a generic chat error and keeps the saved question.

References checked for this integration: [Responses text generation](https://developers.openai.com/api/docs/guides/text), [GPT-6 Luna](https://developers.openai.com/api/docs/models/gpt-6-luna).

## Verification

Run repeatable service checks without a real API key or network calls to OpenAI:

```powershell
dotnet run --project Tests/AIChat.Tests/AIChat.Tests.csproj
```

The fake HTTP handler verifies DEMO makes zero requests, BMI/keyword matching, HTTP 429 fallback, disabled fallback, model/history/role payload, multiple text outputs, refusal, empty output, and missing-key fallback.

Local SQL Express and HTTP form checks passed on 2026-10-15:

- Customer login, chat form, antiforgery rejection, empty/overlength message validation.
- New chat stayed empty; valid first question created conversation 1; subsequent questions reused it.
- BMI response and latest `HYPERTROPHY_BEGINNER_4D` plan appeared in rendered chat and SQL history.
- SQL contained three ordered USER/ASSISTANT pairs. Refresh restored history; opening a new chat did not load old messages.
- Razor encoded submitted script markup. Missing conversations returned 404; another customer could neither read nor send to the test conversation, and those rejected calls changed no records.

The test conversation remains in the local database. Real paid OpenAI requests and interactive browser automation were not run. Existing NU1902/NU1903 dependency warnings remain for System.Security.Cryptography.Pkcs 9.0.11.
