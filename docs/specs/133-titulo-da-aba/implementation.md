# Implementação: título da aba do navegador (#133)

## Causa raiz (investigada, não é pra DeepSeek redescobrir)

`useTituloDaAba` (`frontend/src/lib/useTituloDaAba.ts`) chama
`useNavigation().setOptions({ title })`. Isso é a integração correta em
teoria — o próprio `expo-router` embute um `useDocumentTitle` que escreve
em `document.title` reagindo a esse mesmo `setOptions` (ver
`node_modules/expo-router/build/fork/useDocumentTitle.js`). O problema é
outro: `node_modules/expo-router/build/qualified-entry.js` (o entrypoint
real do bundle web, carregado antes de qualquer código do app) SEMPRE
envolve a árvore inteira em `<Head.Provider>` — que é o `HelmetProvider`
de `react-helmet-async`, vendorizado em
`node_modules/expo-router/vendor/react-helmet-async` — independente de o
app usar `<Head>` explicitamente ou não. Como nenhuma tela deste projeto
renderiza `<Head><title>`, o Helmet mantém seu próprio estado de `<title>`
vazio e o reconcilia no DOM a cada commit — sobrescrevendo, silenciosamente,
qualquer `document.title` setado por fora dele (inclusive o
`useDocumentTitle` do próprio `expo-router`, que não sabe que o Helmet
está ativo). Resultado: `<title></title>` sempre vazio no HTML renderizado
(confirmado via Playwright: `page.title()` retorna `""` tanto na Home
quanto no Login, mesmo o Login passando `titulo="Entrar"` pro `Topbar`) —
um `<title>` vazio é o que faz o navegador cair para mostrar o endereço na
aba, batendo com o relato do usuário.

**A correção não é "consertar" o `useDocumentTitle`** (ele está fazendo
exatamente o que documenta) — é parar de brigar com o Helmet e passar a
alimentá-lo pelo canal que ele espera: renderizar `<Head><title>` de
verdade, usando `expo-router/head` (`import Head from 'expo-router/head'`).
Isso funciona tanto no web (Helmet reconcilia o DOM normalmente) quanto no
native — `node_modules/expo-router/build/head/ExpoHead.android.js` e
`ExpoHead.ios.js` (build padrão Expo Go, sem o módulo nativo de
Activities) renderizam `null`, então `<Head>` é sempre seguro de
renderizar incondicionalmente, sem guard de `Platform.OS`.

## Arquivos afetados

### `frontend/src/lib/useTituloDaAba.ts` → renomeado para `frontend/src/lib/TituloDaAba.tsx`

Deixa de ser hook (`useNavigation().setOptions`) e vira componente (só
componente pode renderizar `<Head>`). Delete o arquivo antigo e o teste
antigo (`useTituloDaAba.test.ts`) — substituídos pelos novos abaixo.

```tsx
import Head from 'expo-router/head';

export type TituloDaAbaProps = { titulo: string };

/**
 * Define o título da aba do navegador (issue #81, causa raiz corrigida na
 * #133 — ver implementation.md da issue: expo-router sempre monta
 * Head.Provider/react-helmet-async por baixo, e só <Head><title>
 * sobrescreve o título de forma confiável; document.title imperativo é
 * pisado pelo Helmet). No native, <Head> é no-op — seguro renderizar
 * incondicionalmente, sem checar Platform.OS.
 */
export function TituloDaAba({ titulo }: TituloDaAbaProps) {
  const tituloCompleto = titulo.startsWith('Synclass') ? titulo : `Synclass - ${titulo}`;
  return (
    <Head>
      <title>{tituloCompleto}</title>
    </Head>
  );
}
```

### `frontend/src/components/organisms/Topbar.tsx`

**Antes** (linhas 1-8, 40-41):
```tsx
import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Marca } from '@/components/atoms/Marca';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { useTituloDaAba } from '@/lib/useTituloDaAba';
import { AlvoDeToqueMinimo, Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  titulo?: string;
  children?: ReactNode;
  menuNavegacao?: ReactNode;
};

export function Topbar({ titulo, children, menuNavegacao }: TopbarProps) {
  useTituloDaAba(titulo ?? 'Synclass');
  const telaLarga = useIsTelaLarga();

  return (
    <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      {/* ...resto do JSX inalterado... */}
    </View>
  );
}
```

**Depois**: troca o import, adiciona a prop nova `tituloDaAba` (só pro
título da aba, quando a tela mostra a variante marca — sem `titulo` — mas
ainda quer um título de aba distinto, ex: Home/Painel), e envolve o `View`
raiz num Fragment pra caber o `<TituloDaAba>` como irmão:

```tsx
import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Marca } from '@/components/atoms/Marca';
import { TituloDaAba } from '@/lib/TituloDaAba';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { AlvoDeToqueMinimo, Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  titulo?: string;
  /** Título da aba do navegador para quando a tela usa a variante marca
   *  (sem `titulo` visível, ex: Home/Painel) mas ainda precisa de um
   *  título de aba distinto do fallback genérico. Ignorado se `titulo`
   *  estiver presente — nesse caso `titulo` já serve pros dois papéis. */
  tituloDaAba?: string;
  children?: ReactNode;
  menuNavegacao?: ReactNode;
};

export function Topbar({ titulo, tituloDaAba, children, menuNavegacao }: TopbarProps) {
  const telaLarga = useIsTelaLarga();

  return (
    <>
      <TituloDaAba titulo={tituloDaAba ?? titulo ?? 'Synclass'} />
      <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
        {/* ...resto do JSX inalterado, sem nenhuma outra mudança... */}
      </View>
    </>
  );
}
```

Não mexe em mais nada do corpo de `Topbar` (`BotaoVoltar`, `Titulo`,
`Logotipo` ficam idênticos).

### `frontend/src/components/organisms/TopbarAutenticada.tsx`

Só repassa a prop nova, mesmo padrão de `titulo`/`children` já existente:

```tsx
export type TopbarAutenticadaProps = {
  titulo?: string;
  tituloDaAba?: string;
  children?: ReactNode;
};

export function TopbarAutenticada({ titulo, tituloDaAba, children }: TopbarAutenticadaProps) {
  const { papeis, papelAtivo, definirPapelAtivo } = useSessao();

  return (
    <Topbar
      titulo={titulo}
      tituloDaAba={tituloDaAba}
      menuNavegacao={
        <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
      }
    >
      {children}
    </Topbar>
  );
}
```

### `frontend/src/app/painel/index.tsx`

Único ponto de uso hoje sem `titulo` nem `tituloDaAba` — troca
`<TopbarAutenticada>` (linha ~51) por `<TopbarAutenticada tituloDaAba="Painel">`,
sem mudar mais nada (continua sem mostrar título visível, só marca — só o
título da ABA muda).

### `frontend/src/components/templates/HomeTemplate.tsx` (não `app/index.tsx`)

**Correção do plano original** (ver "Inconsistências encontradas" em
`task.md`): `app/index.tsx` não usa `Topbar` diretamente, mas
`HomeTemplate` (o template que ele monta) já renderiza seu PRÓPRIO
`<Topbar>` internamente, sem `titulo`. Um `<TituloDaAba>` solto em
`app/index.tsx`, irmão de `<HomeTemplate>`, criaria DOIS `TituloDaAba`
montados na mesma tela — o de dentro do `Topbar` (sem `tituloDaAba`,
fallback `'Synclass'` puro) corre contra o nosso e vence. A correção
certa é passar `tituloDaAba="Início"` pro `Topbar` que já existe dentro
de `HomeTemplate`, não criar um segundo:

**Antes** (`HomeTemplate.tsx`):
```tsx
      <Topbar>
        <Text onPress={onLogin} className="text-sm font-semibold text-primary dark:text-dark-primary">
          Entrar com código ou e-mail
        </Text>
      </Topbar>
```

**Depois**:
```tsx
      <Topbar tituloDaAba="Início">
        <Text onPress={onLogin} className="text-sm font-semibold text-primary dark:text-dark-primary">
          Entrar com código ou e-mail
        </Text>
      </Topbar>
```

`app/index.tsx` não muda nada.
```

Os demais 15 call sites de `Topbar`/`TopbarAutenticada` (todos já passam
`titulo="..."`, ver lista completa no task.md) **não precisam de nenhuma
mudança** — o fallback `tituloDaAba ?? titulo ?? 'Synclass'` já cobre
esses casos usando o `titulo` que já passam.

## Testes

### `frontend/src/lib/TituloDaAba.test.tsx` (novo, substitui `useTituloDaAba.test.ts`)

Mocka `expo-router/head` capturando o que foi passado, em vez de tentar
consultar `document.title` diretamente (mais direto, sem depender do
Helmet realmente montado em teste):

```tsx
import { render } from '@testing-library/react-native';

import { TituloDaAba } from './TituloDaAba';

const mockHead = jest.fn();
jest.mock('expo-router/head', () => ({
  __esModule: true,
  default: (props: { children: React.ReactElement }) => {
    mockHead(props);
    return null;
  },
}));

describe('TituloDaAba', () => {
  beforeEach(() => mockHead.mockReset());

  it('renderiza <title> prefixado por "Synclass - "', () => {
    render(<TituloDaAba titulo="Valor devido" />);

    const children = mockHead.mock.calls[0][0].children;
    expect(children.type).toBe('title');
    expect(children.props.children).toBe('Synclass - Valor devido');
  });

  it('não duplica o prefixo se o título já começar com Synclass', () => {
    render(<TituloDaAba titulo="Synclass - Início" />);

    const children = mockHead.mock.calls[0][0].children;
    expect(children.props.children).toBe('Synclass - Início');
  });
});
```

### `frontend/src/components/organisms/Topbar.test.tsx`

Remove o mock de `useNavigation`/`mockSetOptions` (linhas 9, 14, 26 —
`Topbar` não usa mais nenhum dos dois) e os dois testes que dependiam
deles (linhas ~124-134: "sets the browser tab title to..."). Adiciona
mock de `TituloDaAba` (padrão já usado neste arquivo pra outros
sub-componentes) e dois testes novos equivalentes:

```tsx
const mockTituloDaAba = jest.fn();
jest.mock('@/lib/TituloDaAba', () => ({
  TituloDaAba: (props: { titulo: string }) => {
    mockTituloDaAba(props);
    return null;
  },
}));

// ...dentro do describe, no beforeEach: mockTituloDaAba.mockReset();

it('passes titulo as the tab title when given (issue #81/#133)', async () => {
  await render(<Topbar titulo="Valor devido por Aluno" />);

  expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Valor devido por Aluno' });
});

it('falls back to tituloDaAba when given and titulo is absent (issue #133)', async () => {
  await render(<Topbar tituloDaAba="Painel" />);

  expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Painel' });
});

it('falls back to the bare Synclass tab title when neither titulo nor tituloDaAba is given', async () => {
  await render(<Topbar />);

  expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Synclass' });
});
```

### `frontend/src/components/organisms/TopbarAutenticada.test.tsx`

Adiciona um teste confirmando que `tituloDaAba` é repassado ao `Topbar`
mockado (mesmo padrão que o arquivo já usa pra `titulo`/`children` —
ver os testes existentes de repasse de prop nesse arquivo antes de
escrever o novo, pra usar a mesma forma de mock do `Topbar`).

### `frontend/src/app/painel/index.test.tsx`

`TopbarAutenticada` já é mockado neste arquivo como um stub que só
renderiza `children` (ver topo do arquivo) — troca o mock por uma versão
que também captura as props recebidas, e adiciona um teste:

```tsx
const mockTopbarAutenticada = jest.fn();
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: (props: { children?: React.ReactNode; tituloDaAba?: string }) => {
      mockTopbarAutenticada(props);
      return <View>{props.children}</View>;
    },
  };
});

it('sets the tab title to Painel (issue #133)', async () => {
  // ...mesmo setup de useSessao/buscarPerfil já usado nos outros testes deste arquivo...
  await render(<PainelScreen />);

  expect(mockTopbarAutenticada).toHaveBeenCalledWith(expect.objectContaining({ tituloDaAba: 'Painel' }));
});
```

### `frontend/src/app/index.test.tsx` (Home)

Adiciona mock de `TituloDaAba` (`HomeTemplate` não é mockado neste
arquivo hoje — não mude isso) e um teste:

```tsx
const mockTituloDaAba = jest.fn();
jest.mock('@/lib/TituloDaAba', () => ({
  TituloDaAba: (props: { titulo: string }) => {
    mockTituloDaAba(props);
    return null;
  },
}));

it('sets the tab title to Início (issue #133)', async () => {
  await render(<HomeScreen />);

  expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Início' });
});
```

## Verificação visual/manual (além dos testes)

Depois do gate de testes verde, confirme com Playwright (mesmo padrão já
usado nesta sessão): suba `npx expo start --web`, `page.title()` em pelo
menos `/`, `/login`, `/painel` (autenticado) — espera-se
`"Synclass - Início"`, `"Synclass - Entrar"`, `"Synclass - Painel"`
respectivamente, nunca `""` nem o host/porta.

## Fora de escopo

- `+not-found.tsx` (rota 404, se existir) — não citado nos critérios de
  aceite, não mexer.
- Meta tags de SEO/Open Graph (`<meta property="og:...">`) — só o
  `<title>` está em escopo aqui.
