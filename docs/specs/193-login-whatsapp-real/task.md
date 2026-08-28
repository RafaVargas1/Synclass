
# Task: Entrega de código OTP por WhatsApp (#193)

Card: https://github.com/RafaVargas1/Synclass/issues/193

## Ordem de execução

- [ ] Teste unidade (Domain): `INotificador.EnviarCodigoAsync` recebe contato em E.164 e código; sucesso — mensagem contém código, sem retorno de erro
- [ ] Teste unidade (Domain): `INotificador.EnviarCodigoAsync` recebe falha do provedor (HttpRequestException/TimeoutException); exceção `OtpEnvioException` com motivo sem detalhe técnico
- [ ] Implementação mínima: `WhatsAppNotificador` com `IWhatsAppHttpClient` mockado (sucesso e falha) — sem rede real
- [ ] Teste unidade (Domain): `TelefoneUtils.NormalizarParaE164` converte formatos variados (com espaço, hífen, com/sem +55) para E.164
- [ ] Implementação mínima: `TelefoneUtils.NormalizarParaE164` — edge point explícito do card (RN #193)
- [ ] Teste unidade (Domain): `OtpEnviado` (evento de sucesso) não inclui código OTP; `OtpEnvioFalhou` (evento de falha) inclui motivo mas não código
- [ ] Implementação mínima: eventos estruturados `OtpEnviado` e `OtpEnvioFalhou` — substituem `OtpEnviadoParaDesenvolvimento` para este fluxo
- [ ] Implementação `Infrastructure`: `WhatsAppHttpClient` (wrapper de `HttpClient` com timeout curto) e `WhatsAppNotificador` concreto — resolve DI em `Program.cs` (fábrica substituível)
- [ ] Implementação `Api`: injeta o `INotificador` real no `LoginController` (substitui `NotificadorDeLog`, que fica apenas para ambiente de dev)
- [ ] Configuração: `Program.cs` valida `WhatsApp:ApiKey` e `WhatsApp:NumeroRemetente` no startup (falha explícita se obrigatório — padrão `Jwt:SigningKey`); adiciona sem valor a `.env.example`
- [ ] Teste de fumaça (Api): `POST /api/login/solicitar-codigo` com contato inválido — retorna 400 `ContatoInvalidoException`; com provedor mockado de falha — retorna 502 `OtpEnvioException` sem detalhe técnico
- [ ] Log estruturado: verificar nos logs que `OtpEnviado` e `OtpEnvioFalhou` não contêm o código em texto puro; `OtpEnviadoParaDesenvolvimento` (com código) só aparece quando `AssinaturaDigital:ModoDev=true`
- [ ] Componente frontend: **nenhum** — fluxo permanece na `LoginScreen.tsx` existente, exibindo o erro retornado pela API (prop `message` do componente `ErrorState` já existente; sem alteração de layout)

**Questão aberta (não bloqueia implementação, é decisão de provedor)**: escolher entre Twilio WhatsApp API e WhatsApp Business Cloud API — afeta `WhatsAppHttpClient` (rota e formato do payload). A implementação parte com **Twilio como padrão** (SDK maduro, timeout declarável), mas `IWhatsAppHttpClient` é a fronteira para troca sem tocar `INotificador` ou `Program.cs` além da fábrica.

