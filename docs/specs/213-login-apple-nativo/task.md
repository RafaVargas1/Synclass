# Task: Login com Apple no app nativo (iOS) (#213)

Card: https://github.com/RafaVargas1/Synclass/issues/213

## Ordem de execução

- [x] Teste unidade (Domain, backend): `ValidadorDeIdTokenApple.ValidarAsync` aceita token com `aud` igual ao Bundle ID nativo (`br.com.synclass.app`), além do Services ID web já suportado — ver `implementation.md#edge-points` (sem essa mudança, todo login nativo falha a validação de audience mesmo com token genuíno)
- [x] Implementação (backend): trocar `ValidAudience` por `ValidAudiences` (coleção) em `ValidadorDeIdTokenApple`, com os dois valores configurados (`AppleClientId` do web + `AppleBundleId` nativo, novo em `.env.example`/`IConfiguration`)
- [x] Teste unidade: mock de `expo-apple-authentication`, cobrindo `obterIdTokenNativoApple()` com sucesso (retorna `identityToken`), cancelamento (`error.code === 'ERR_REQUEST_CANCELED'` mapeado para `null`) e e-mail ausente (login que não é o primeiro — ver `implementation.md#edge-points`)
- [x] Implementação: `frontend/src/lib/auth/apple.ts` — `obterIdTokenApple()` ganha ramo nativo (hoje só cobre web, `Platform.OS !== 'web'` sempre devolve `null`), espelhando exatamente como `google.ts` ramifica `obterIdTokenNativo()` vs a versão web
- [x] Configuração: `app.json` — plugin `expo-apple-authentication` + `ios.usesAppleSignIn: true`
- [x] Componente frontend: botão nativo usa `AppleAuthentication.AppleAuthenticationButton` (componente pronto do SDK, não o `Button` genérico nem o `BotaoLoginApple.tsx` da web) — só renderiza quando `Platform.OS === 'ios'` (nunca no Android, ver RN do card). Ver `implementation.md#componente-de-botão-nativo` pro ponto de inserção exato
- [x] Teste: componente do botão nativo só renderiza em iOS (mock de `Platform.OS`)
- [x] Suíte de testes completa (frontend) verde antes do PR

## Fora do escopo de código (ação manual, não travar a Task por causa disso)

- `eas build` de verdade precisa de uma conta Apple Developer paga vinculada ao projeto EAS pra registrar a capability "Sign in with Apple" no App ID (`br.com.synclass.app`) e gerar o provisioning profile — isso acontece no momento do build, não no código. Diferente de #192 (Google), aqui **não há nenhum Client ID pra criar/colar manualmente antes** — o código pode ficar 100% pronto e mergeado sem essa conta existir ainda.
