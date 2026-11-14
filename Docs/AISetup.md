# WinClip AI Configuration

WinClip supports three AI providers for text processing actions (Summarize, Grammar Check, Rephrase, Expand, Custom Prompt). Configure via **Settings > AI Features** in the app.

---

## LM Studio (Local)

Run AI models locally with no API key required.

| Setting | Value |
|---------|-------|
| Provider | `lmstudio` |
| Endpoint | `http://localhost:1234` (default LM Studio port — **do not include `/v1`**, WinClip appends it) |
| API Key | Optional (only if LM Studio is configured with auth) |
| Model | Exact model ID as shown in LM Studio's loaded models list (e.g., `qwen2.5-7b-instruct`). Leaving this blank or set to `default` routes to the currently active model. |

**Setup:**
1. Install [LM Studio](https://lmstudio.ai/)
2. Download and load a model — prefer non-reasoning models for fast responses (e.g., `qwen2.5-7b-instruct`, `llama-3.1-8b-instruct`). Reasoning models like `qwen3.x` can take 60+ seconds per request.
3. Start the local server (Developer / Local Server tab, default port 1234)
4. In WinClip Settings > AI, select LM Studio and set the endpoint to `http://localhost:1234`
5. Restart WinClip after saving — the AI service binds at startup.

**Tuning timeouts:** WinClip waits up to 180 seconds by default for LM Studio responses. If you have very slow hardware or large models, raise `AI:LMStudio:TimeoutSeconds` in `appsettings.json`. If you want to fail faster during testing, lower it.

---

## OpenAI

| Setting | Value |
|---------|-------|
| Provider | `openai` |
| API Key | `sk-...` from [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Model | `gpt-4o` (default), `gpt-4o-mini`, `gpt-4`, `gpt-3.5-turbo` |

**Setup:**
1. Create an API key at OpenAI's platform
2. In WinClip Settings > AI, select OpenAI and paste the key

---

## Claude (Anthropic)

| Setting | Value |
|---------|-------|
| Provider | `claude` |
| API Key | `sk-ant-...` from [console.anthropic.com](https://console.anthropic.com/) |
| Model | `claude-sonnet-4-20250514` (default), or any Claude model ID |

**Setup:**
1. Create an API key at Anthropic's console
2. In WinClip Settings > AI, select Claude and paste the key

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Set up AI in Settings > AI Features" | No provider configured. Open Settings and select a provider. |
| "AI service returned empty result" | The model returned a blank response. Try a different model or check the prompt. |
| Timeout errors | Default timeout is 180 seconds for LM Studio. Reasoning models (qwen3.x, deepseek-r1) can exceed this — switch to a non-reasoning model or raise `AI:LMStudio:TimeoutSeconds` in `appsettings.json`. |
| "Cancelled" message on AI actions | Almost always an HTTP timeout in disguise (now reported clearly as of v1.2.1). Try a faster model. |
| 404 errors on LM Studio | Check the endpoint — it should be `http://localhost:1234`, **not** `http://localhost:1234/v1`. WinClip auto-strips a trailing `/v1` as of v1.2.1, but earlier builds will 404. |
| AI settings change not taking effect | The AI service is a singleton — fully restart WinClip after changing provider, endpoint, or model. |
| "AI service is not configured or unavailable" | API key is missing or invalid. Verify the key in Settings. |
| LM Studio connection refused | Ensure LM Studio's local server is running and the endpoint URL is correct. |
| 401 / 403 errors | API key is invalid or expired. Generate a new key from the provider. |
| Rate limit errors | You've exceeded the provider's API rate limit. Wait a moment and try again. |
