
# Implementation: Login com Google no app nativo (Android/iOS) (#192)

## Entidades/classes afetadas

### Frontend — mudança principal

- **`frontend/src/lib/auth/google.ts`** — única mudança de lógica.
  - Função: `obterIdTokenNativo()` (linha ~35, hoje retorna `null` sempre, com stub proposital).
  - **Não muda**: `obterIdTokenGoogle()` (ramifica por `Platform.OS`, chama `obterIdTokenNativo()` no nativo) — contrato do arquivo é "nunca lança, devolve `null` em falha/cancelamento", preservado.

### Frontend — configuração

- **`app.json`** — adicionar plugin `@react-native-google-signin/google-signin` (ver seção Configuração abaixo).
- **`.env.example`** (raiz do repo — não existe `frontend/.env.example`, confirmado por busca) — adicionar `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID` (valor vazio). Confirmar se `EXPO_PUBLIC_GOOGLE_CLIENT_ID` (o web client id, já usado pelo fluxo web da issue #65) já está documentado ali — se não estiver, adicionar também (débito de #65, mas corrija se notar a lacuna). `EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID` só é necessário se a implementação confirmar que o Android precisa de um client id próprio além do `webClientId` (ver "Edge points" abaixo — não confirmado).

### Backend

- **Nenhuma mudança.** `POST /auth/google` e `ValidadorDeIdTokenGoogle` já tratam o fluxo (issue #65). O campo `aud` do idToken é validado contra `GOOGLE_CLIENT_ID` do backend — que é o MESMO valor de `EXPO_PUBLIC_GOOGLE_CLIENT_ID` no frontend web.

## Ponto de inserção exato

### Antes (trecho atual de `frontend/src/lib/auth/google.ts` — linha ~35):

```typescript
export async function obterIdTokenNativo(): Promise<string | null> {
  // TODO(#192): fluxo nativo Google (expo-auth-session ou @react-native-google-signin).
  // Sempre null até o suporte a Android/iOS ser implementado.
  return null;
}
```

### Depois (desenho — não é texto literal, veja a ressalva de API abaixo antes de codar)

> **ATENÇÃO — forma exata da API não confirmada com certeza (ver `task.md#inconsistências-encontradas`, item 3)**: o exemplo abaixo assume `configure()` síncrono e `signIn()` lançando exceção com `error.code === statusCodes.SIGN_IN_CANCELLED` no cancelamento (padrão histórico da lib). **Antes de implementar, confira o typings real instalado** (`node_modules/@react-native-google-signin/google-signin/lib/typescript/*.d.ts` após o `expo install`) — versões mais recentes (v13+, Credential Manager API no Android) trocaram esse padrão por um retorno de união discriminada (`{ type: 'success', data: {...} } | { type: 'cancelled' }`, sem lançar no cancelamento). Adapte o código ao que o typings real mostrar — **não** assuma o exemplo abaixo como verdade absoluta, ele é o desenho de referência de um padrão documentado publicamente, mas não a versão exata que o `expo install` vai baixar.

```typescript
import { GoogleSignin, statusCodes } from '@react-native-google-signin/google-signin';

GoogleSignin.configure({
  webClientId: process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID,
  iosClientId: process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID,
});

export async function obterIdTokenNativo(): Promise<string | null> {
  try {
    const resultado = await GoogleSignin.signIn();
    return resultado.idToken ?? null; // ajuste o acesso ao idToken à forma real do retorno (ver ressalva acima)
  } catch (error) {
    if (error.code === statusCodes.SIGN_IN_CANCELLED) {
      return null;
    }
    // Outros erros também viram null — contrato do arquivo nunca lança.
    return null;
  }
}
```

**Decisões**:
- `configure()` chamado **uma vez, no escopo do módulo** (fora da função, no topo de `google.ts`) — não em cada chamada de `obterIdTokenNativo()`, seguindo a recomendação da lib de configurar uma única vez por processo do app.
- `androidClientId` **não é passado** no `configure()` — o Android usa o `webClientId` para o handshake e para o `aud` do idToken; passar um `androidClientId` seria redundante e a doc não pede. Se o teste real revelar que é necessário, documente o porquê aqui antes de adicionar.

### No meio (nada entre o antes e o depois — `obterIdTokenGoogle()` não muda)

## Contrato de API

**Nenhum contrato novo.** `POST /auth/google` já recebe:

```
Request: { idToken: string }
Response 200: { token: string, aluno?: {...}, professor?: {...} }
Response 401: { erro: "CadastroPendente", mensagem: string }
```

Frontend já consome (ver `frontend/src/lib/api/auth.ts`, função `loginComGoogle`).

## Modelo de dados

**Nenhuma migration nova.** Não toca tabela do backend.

## Edge points não cobertos por critério de aceite

- **`androidClientId` no `configure()`**: a doc da lib diz que Android usa o `webClientId` para o idToken e o `androidClientId` é opcional (usado quando o backend valida o token do Android de forma específica). Como o backend espera `aud` = `GOOGLE_CLIENT_ID` web, **não passar** `androidClientId` evita risco de token com `aud` errado. Decisão: só `webClientId` + `iosClientId`. **A confirmar na implementação** com o teste real.
- **Acesso à conta EAS**: `eas.json` usa "environment" (contexto #3) — variáveis novas precisam ser adicionadas via EAS dashboard por quem tem acesso, não no `.env` local, para os builds de preview/production funcionarem. Isso é **operação**, não código — menciono aqui para não virar surpresa no deploy.
- **Expo Go não suporta a lib** (contexto #4): `obterIdTokenNativo()` vai falhar/retornar `null` no Expo Go. Como o contrato do arquivo é "nunca lança", o app continua funcionando sem o botão Google no Expo Go (o botão já não fazia nada antes). Teste em development build (de acordo com #195).

## Padrão de estilo a seguir

- **Mock de SDK nativo em Jest**: procurar exemplo real de `jest.mock` de SDK nativo no repo antes de escrever — contexto #6 menciona "padrão já usado no repo", ex: `expo-secure-store`. Se não houver exemplo, usar o padrão do proprio jest:
  ```typescript
  jest.mock('@react-native-google-signin/google-signin', () => ({
    GoogleSignin: { configure: jest.fn(), signIn: jest.fn() },
    statusCodes: { SIGN_IN_CANCELLED: 'SIGN_IN_CANCELLED' },
  }));
  ```
- **Log estruturado**: nenhum código novo — ver `docs/architecture.md#logs-estruturados-e-track-id` e os eventos existentes no fluxo web (`google.ts`/`useAutenticadoGoogle.ts`) para confirmar que `GoogleLoginSolicitado`/`GoogleLoginSucesso` já cobrem o nativo sem mudança.

## Dependência de outras Tasks

- **Issue #65** (login com Google web): pré-requisito — backend e fluxo web já validados. Este card é a continuação nativa.
- **Issue #195** (identidade/config), mergeada: `bundleIdentifier`/`package` = "br.com.synclass.app", scheme "synclass" no `app.json`, eas.json com perfis dev/preview/production usando "environment". A `iosUrlScheme` do plugin da lib deve seguir o padrão reverso do iOS client ID — se o bundleIdentifier for `br.com.synclass.app`, o client ID iOS do Google Cloud Console deve ser `com.googleusercontent.apps.<ios-client-id>` — necessário configurar no console, não só no código.

## Verificações compulsórias antes de terminar

1. **Compatibilidade Expo**: rodar `npx expo install @react-native-google-signin/google-signin` (em `frontend/`) — o `expo install` baixa a versão compatível com o SDK atual (não `npm install` direto). Se falhar ou mudar de versão major, **parar** e consultar `frontend/AGENTS.md` (Expo HAS CHANGED).
2. **Leitura do arquivo real**: `frontend/src/lib/auth/google.ts` — confirmar que `obterIdTokenGoogle()` realmente chama `obterIdTokenNativo()` e que o stub é como descrito (não inventar o código atual).
3. **Teste do caminho de cancelamento**: o teste precisa mapear o erro real da lib (`SIGN_IN_CANCELLED`) para `null` — se a lib em versão atual usar código diferente, ajustar.
4. **`webClientId` no configure**: precisa ser o mesmo `EXPO_PUBLIC_GOOGLE_CLIENT_ID` — se o plugin da lib oferecer "webClientId" como opção de config no `app.json`, definir lá também ou garantir que `configure()` em runtime é suficiente (preferir runtime).
