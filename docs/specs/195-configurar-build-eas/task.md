# Task: Configurar build nativo com EAS (#195)

Card: https://github.com/RafaVargas1/Synclass/issues/195

Task trivial (config, sem lógica de negócio/testável em unidade) — checklist mecânico direto dos critérios do card, sem `implementation.md`.

## Ordem de execução

- [x] `frontend/app.json`: adiciona `ios.bundleIdentifier`/`ios.buildNumber` e `android.package`/`android.versionCode` (ausentes — obrigatórios pra `eas build` funcionar)
- [x] `frontend/app.json`: corrige `ios.icon` — apontava pra `./assets/expo.icon`, que é o símbolo padrão do Expo (não o logo do Synclass), então builds iOS mostrariam ícone placeholder mesmo com Android/web já usando o ícone certo (`assets/images/icon.png`, gráfico de barras azul). Removida a sobrescrita — iOS cai no `icon` do nível raiz, igual às outras plataformas
- [x] `frontend/eas.json` (não existia): perfis `development`/`preview`/`production`, usando `environment` (variáveis EAS geridas pelo dashboard/CLI da conta Expo, não um bloco `env` com valor commitado — consistente com a convenção do repo de nunca commitar segredo real; confirmado via docs.expo.dev/eas/environment-variables que essa é a forma atual recomendada, não o `env` inline legado)
- [ ] **Fora do escopo de código, exige ação manual do responsável (ver card, "Contexto ou protótipo")**: criar/logar numa conta Expo/EAS, rodar `eas login`, configurar as variáveis `EXPO_PUBLIC_API_URL`/`EXPO_PUBLIC_APP_URL`/`EXPO_PUBLIC_GOOGLE_CLIENT_ID` por ambiente (`eas env:create --environment development|preview|production`), e rodar `eas build --platform android --profile preview` / `--platform ios --profile preview` pela primeira vez
- [ ] Teste de fumaça manual em dispositivo real (login, menu lateral, uma ação de cada papel) — só possível depois do primeiro build gerado, fora do escopo desta Task de código

## Observação (não bloqueia esta Task, registrar para follow-up)

Não existe hoje nenhum `.env.example` documentando as variáveis `EXPO_PUBLIC_*` (`API_URL`, `APP_URL`, `GOOGLE_CLIENT_ID`) usadas pelo frontend — só `.env.example` da raiz (backend/infra). Como a Task adota `environment` (variáveis geridas pela conta EAS, não arquivo), isso não é pré-requisito de código pra `eas build` funcionar — mas para desenvolvimento local (`npm start`) documentar essas chaves ajudaria quem configurar o projeto do zero. Fora do escopo deste card (nenhum critério de aceite pede isso).

## Bundle identifier escolhido

`br.com.synclass.app` — reverse-DNS do domínio já usado como referência em specs anteriores (`api.synclass.com.br`, #199/#200/#203). Mesmo identificador pras duas plataformas (convenção comum, não exigido).
