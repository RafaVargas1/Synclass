# Task: Login com Google no app nativo (Android/iOS) (#192)

Card: https://github.com/RafaVargas1/Synclass/issues/192

## Ordem de execução

- [ ] Teste unidade: mock do SDK `@react-native-google-signin/google-signin`, cobrindo `obterIdTokenNativo()` com sucesso (retorna idToken) e cancelamento (mapeia `SIGN_IN_CANCELLED` para `null`)
- [ ] Implementação mínima em `frontend/src/lib/auth/google.ts`: substituir stub de `obterIdTokenNativo()` pela chamada real ao SDK, com `GoogleSignin.configure()` (webClientId = `EXPO_PUBLIC_GOOGLE_CLIENT_ID`, iosClientId) e mapeamento de cancelamento para `null`
- [ ] Teste unidade: `useAutenticadoGoogle` com caminho nativo (mock do SDK + mock de `obterIdTokenGoogle` retornando idToken) — cobre que a sessão é criada e chama o backend
- [ ] Configuração: adicionar plugin no `app.json` para `@react-native-google-signin/google-signin` com `iosUrlScheme` no formato reverso do iOS client ID
- [ ] Configuração: adicionar variáveis de ambiente ao `.env.example` — `EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID` e `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID` (valores vazios, nunca committar segredo real)
- [ ] Verificação: compatibilidade da lib com Expo SDK 57 (a versão real do projeto, `frontend/package.json` — não SDK 53) via `npx expo install @react-native-google-signin/google-signin` (nunca `npm install` direto, ver `frontend/AGENTS.md`) — documentar resultado em `implementation.md`
- [ ] Log estruturado: **nenhum código novo** — reutilizar `GoogleLoginSolicitado`/`GoogleLoginSucesso` existentes do fluxo web (ver `frontend/src/lib/auth/google.ts` e `useAutenticadoGoogle.ts` para nomes exatos dos eventos)

> **Nota sobre item de frontend vago**: este card não tem item de UI nova — o botão "Entrar com Google" (`BotaoLoginGoogle.tsx`) já existe e é agnóstico de plataforma. A única mudança é o caminho nativo em `obterIdTokenNativo()`, que não altera visual.

## Critérios de aceite cobertos

| Critério (do card) | Onde o teste cobre | Onde a implementação acontece |
|---|---|---|
| Sucesso: sessão criada, cai no Painel | Teste de `useAutenticadoGoogle` (caminho nativo) | `obterIdTokenNativo()` real + fluxo existente |
| E-mail sem conta: mostra "cadastro pendente" | Nenhum teste novo necessário — mesmo retorno do backend, mesmo tratamento do fluxo web já testado | Nenhuma mudança (backend rejeita, UI já trata `CadastroPendente: true`) |
| Cancelamento: volta ao estado anterior sem erro/tela travada | Teste de `obterIdTokenNativo()` (mapa `SIGN_IN_CANCELLED` → `null`) | Mapeamento no adaptador nativo |

## Inconsistências encontradas (resolvidas por Claude antes da implementação começar)

1. **Rascunho citava "Expo SDK 53"** — errado, o projeto está na SDK 57 (`frontend/package.json`, `expo: ~57.0.18`). Corrigido no item de verificação de compatibilidade acima.
2. **`.env.example` novas variáveis, destino não decidido no rascunho**: não existe `frontend/.env.example` no repo (confirmado por busca) — as únicas variáveis relacionadas ao Google hoje ficam em `.env.example` da raiz (`GOOGLE_CLIENT_ID`, usada pelo backend). Decisão: `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID` (e, se necessário, `EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID`) vão no `.env.example` da raiz, junto das demais — mesmo lugar de todo o resto, sem criar arquivo novo. `EXPO_PUBLIC_GOOGLE_CLIENT_ID` (o "web client id") **já deveria existir** no frontend hoje (é usado pelo fluxo web, issue #65) — se não estiver documentado no `.env.example` da raiz, adicionar também (débito da issue #65, não desta Task, mas corrija se notar a lacuna).
3. **Forma exata da API do `@react-native-google-signin/google-signin` não confirmada com certeza total**: pesquisa nas docs oficiais confirmou (a) usar essa lib, não `expo-auth-session`; (b) `webClientId` no `configure()` precisa ser o mesmo Client ID "Web application" já usado no backend, para o `aud` do idToken bater com `ValidadorDeIdTokenGoogle`. **Não confirmado com certeza**: se `GoogleSignin.configure()` é síncrono ou retorna Promise, e se `GoogleSignin.signIn()` (na versão que o `expo install` de fato baixar) lança exceção com `error.code === statusCodes.SIGN_IN_CANCELLED` no cancelamento (padrão de versões mais antigas da lib) OU devolve um objeto com união discriminada tipo `{ type: 'cancelled' }` (mudança de API em versões mais recentes, v13+, ligada à nova Credential Manager API do Android) — **verificar contra o código-fonte/typings real da versão instalada antes de escrever o mapeamento de cancelamento**, não assumir nenhum dos dois formatos como certo sem checar. Ver `implementation.md#ponto-de-inserção-exato`.

