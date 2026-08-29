# Desenho técnico — Login com Apple no app nativo (iOS) (#213)

## Entidades/classes afetadas

### Frontend — mudança principal

- **`frontend/src/lib/auth/apple.ts`** — `obterIdTokenApple()` hoje (`#212`, mergeada) só cobre `Platform.OS === 'web'`; para qualquer outra plataforma devolve `null` sem tentar nada. Ganha um `else` chamando um novo `obterIdTokenNativoApple()` — só quando `Platform.OS === 'ios'` (Android continua devolvendo `null`, ver RN do card: "Sign in with Apple" não é exigência de política no Android, e o SDK nativo da Apple só funciona em iOS de qualquer forma).

### Ponto de inserção exato

**Antes** (`frontend/src/lib/auth/apple.ts`, trecho atual — confira linha exata no arquivo real antes de editar):
```typescript
export async function obterIdTokenApple(): Promise<string | null> {
  if (Platform.OS !== 'web' || typeof window === 'undefined') {
    return null;
  }
  // ... fluxo web
}
```

**Depois** (desenho):
```typescript
import * as AppleAuthentication from 'expo-apple-authentication';

export async function obterIdTokenApple(): Promise<string | null> {
  if (Platform.OS === 'ios') {
    return obterIdTokenNativoApple();
  }
  if (Platform.OS !== 'web' || typeof window === 'undefined') {
    return null; // Android: sem Sign in with Apple, nem web nem nativo
  }
  // ... fluxo web (inalterado)
}

async function obterIdTokenNativoApple(): Promise<string | null> {
  try {
    const credential = await AppleAuthentication.signInAsync({
      requestedScopes: [
        AppleAuthentication.AppleAuthenticationScope.FULL_NAME,
        AppleAuthentication.AppleAuthenticationScope.EMAIL,
      ],
    });
    return credential.identityToken ?? null;
  } catch (error) {
    // error.code === 'ERR_REQUEST_CANCELED' no cancelamento — confirmado
    // na doc oficial da Expo (v57). Qualquer outro erro também vira null,
    // mesmo contrato do arquivo inteiro (nunca lança).
    return null;
  }
}
```

O `identityToken` retornado é consumido do mesmo jeito que o `idToken` do fluxo web — `POST /auth/apple` (#212, já pronto) não distingue a origem, mesmo contrato.

## Componente de botão nativo

- **`frontend/src/components/molecules/BotaoLoginApple.tsx`** (de #212, só cobre web hoje) — adicionar um ramo condicional: em iOS, renderiza `AppleAuthentication.AppleAuthenticationButton` (componente pronto do SDK — `buttonType: SIGN_IN`, `buttonStyle: BLACK` claro / branco escuro, mesmo racional de tema claro/escuro já usado no resto do app) chamando o mesmo `onPress` que já dispara `obterIdTokenApple()` → `loginComApple()` (ou nome equivalente já criado por #212 — confira o arquivo real). Em Android, o componente não renderiza nada (`return null`). Em web, mantém o botão atual (inalterado).
- **Não usar o `Button` genérico nem o botão preto/branco HTML da web pro nativo** — a Apple exige o componente oficial `AppleAuthenticationButton` em apps nativos (não um botão customizado), diferente da web onde o HTML customizado seguindo a diretriz visual é aceito.

## Configuração

- `app.json`: adicionar `"expo-apple-authentication"` à lista de `plugins`, e `"usesAppleSignIn": true` dentro de `ios` (já existe `ios.bundleIdentifier`/`ios.buildNumber` de #195 — só adicionar a chave nova, não recriar o bloco).
- **Nenhuma variável de ambiente nova** — diferente do Google nativo (#192, que precisa de um Client ID específico por plataforma), a Apple não exige nenhum client ID adicional pro fluxo nativo: o `identityToken` retornado já tem `aud` = bundle identifier do app (`br.com.synclass.app`), e o backend (#212) valida contra `AppleClientId` (Services ID) só no fluxo **web** — **edge point real, ver abaixo**.

## Edge points não cobertos por critério de aceite Gherkin

- **`aud` do `identityToken` nativo é o bundle identifier, não o Services ID**: o backend (`ValidadorDeIdTokenApple`, #212) valida `ValidAudience` contra `AppleClientId` (o Services ID usado no fluxo web). O token do fluxo **nativo** vem com `aud = br.com.synclass.app` (o App ID), um valor DIFERENTE do Services ID. Se o backend continuar validando só contra um único `AppleClientId`, todo login nativo vai falhar a validação de audience mesmo com token genuíno. **Decisão**: `ValidadorDeIdTokenApple.ValidarAsync` precisa aceitar **qualquer um dos dois** valores de audience configurados (Services ID do web + Bundle ID do app nativo) — trocar `ValidAudience` (singular) por `ValidAudiences` (`TokenValidationParameters` já suporta uma coleção) com os dois valores. Isso é uma mudança pequena em #212 (já mergeada), não uma reabertura de escopo grande — trate como parte desta Task, já que é o que destrava o nativo funcionar de verdade.
- **E-mail ausente em logins após o primeiro**: mesma limitação já documentada em #212 (a Apple só devolve e-mail na primeira autorização) — vale igual pro nativo, sem tratamento adicional aqui.
- **`fullName`/`email` só vêm no PRIMEIRO `signInAsync` bem-sucedido** — em execuções seguintes, `credential.email`/`credential.fullName` podem vir `null` mesmo que `identityToken` continue válido; isso não afeta o backend (que lê o e-mail do JWT, não do objeto `credential`), mas é bom não depender de `credential.email` em nenhum lugar do frontend.

## Dependência de outras Tasks

- **#212** (login Apple web, mergeada): contrato de `POST /auth/apple`, `BotaoLoginApple.tsx`, `useAutenticadoApple.ts` já existem — esta Task estende, não recria. Precisa da mudança de `ValidAudiences` descrita acima.
- **#195** (build EAS, mergeada): `ios.bundleIdentifier` já configurado.
- **Bloqueia #197** (App Store) — junto com #212, satisfaz a Guideline 4.8 da Apple.
