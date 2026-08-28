# Implementation: Padronizar ícones e cores (#202)

## Dependência

- `frontend/package.json` linha 6: `"@expo/vector-icons": "^15.0.2"` — remover
  depois que `MenuNavegacao.tsx` parar de importar `Ionicons` (é o único
  import real do pacote; `secoesPorPapel.ts` só o menciona em comentário).
- Instalar via `npx expo install phosphor-react-native` (não `npm install`
  direto — `expo install` resolve a versão compatível com o Expo SDK 57
  já fixado no projeto, mesma forma usada para `@expo/vector-icons`
  originalmente).

## Tabela de ícones (Ionicons atual -> chave nova -> componente Phosphor)

| Ionicons (hoje) | Chave `NomeIconeSecao` nova | Componente Phosphor | Onde usa |
|---|---|---|---|
| `home-outline` | `home` | `House` | `SecaoPainel` |
| `person-outline` | `perfil` | `User` | `SecaoMeuPerfil` |
| `person-add-outline` | `adicionar-aluno` | `UserPlus` | `secoesProfessor` |
| `clipboard-outline` | `chamada` | `ClipboardText` | `secoesProfessor` |
| `time-outline` | `horarios` | `Clock` | `secoesProfessor` |
| `people-outline` | `alunos` | `Users` | `secoesProfessor` |
| `link-outline` | `alocacoes` | `LinkSimple` | `secoesProfessor` |
| `cash-outline` | `valor-devido` | `CurrencyDollar` | `secoesProfessor`, `secoesAluno` |
| `enter-outline` | `entrar-turma` | `SignIn` | `secoesAluno` |
| `calendar-outline` | `minhas-aulas` | `CalendarBlank` | `secoesAluno` |
| `bar-chart-outline` | `historico` | `ChartBar` | `secoesAluno` |

Ações de tela (novas, RN #3 do card):

| Ação (rótulo visível) | Componente Phosphor | Onde |
|---|---|---|
| "Chamada" | `ClipboardText` (mesmo ícone da seção "Fazer chamada" do menu — reforça que é a mesma ação) | `HorarioCard.tsx` (`CardCorpo`) |
| "Remover" | `Trash` | `HorarioCard.tsx` (`CardCorpo`) |
| "Cancelar" | `X` | `HorarioCard.tsx` (edição inline) e `AulaProximaCard.tsx` |
| "Marcar" | `CalendarPlus` | `HorarioVagoCard.tsx` |
| "Confirmar presença" | `CheckCircle` | `AulaProximaCard.tsx` |

Todos os ícones em peso `regular`, tamanho `20` (inline com texto — nenhum
destes é um botão-somente-ícone standalone, todos acompanham rótulo visível
ao lado). Tamanho `24` fica sem uso nesta Task (não há botão-somente-ícone
sendo criado); documentar isso no `design-system.md` em vez de forçar um
uso artificial de 24px.

## `frontend/src/lib/secoesPorPapel.ts`

**Antes** (linhas 8-19):
```ts
export type NomeIconeSecao =
  | 'home-outline'
  | 'person-outline'
  | 'person-add-outline'
  | 'clipboard-outline'
  | 'time-outline'
  | 'people-outline'
  | 'link-outline'
  | 'cash-outline'
  | 'enter-outline'
  | 'calendar-outline'
  | 'bar-chart-outline';
```

**Depois**:
```ts
export type NomeIconeSecao =
  | 'home'
  | 'perfil'
  | 'adicionar-aluno'
  | 'chamada'
  | 'horarios'
  | 'alunos'
  | 'alocacoes'
  | 'valor-devido'
  | 'entrar-turma'
  | 'minhas-aulas'
  | 'historico';
```

Atualizar o comentário do bloco (linhas 3-7) trocando "conjunto `Ionicons`
(`@expo/vector-icons`)" por "conjunto Phosphor (`phosphor-react-native`)".

Cada `icone: 'home-outline'` etc. nos objetos `Secao` de `secoesAluno`,
`secoesProfessor` muda para a chave nova correspondente da tabela acima
(ex: `icone: 'minhas-aulas'` em vez de `icone: 'calendar-outline'`).

## `frontend/src/components/organisms/MenuNavegacao.tsx`

**Antes** (import, topo do arquivo):
```tsx
import { Ionicons } from '@expo/vector-icons';
```

**Depois**:
```tsx
import { CalendarBlank, ChartBar, ClipboardText, Clock, CurrencyDollar, House, LinkSimple, SignIn, User, UserPlus, Users } from 'phosphor-react-native';
```

**Antes** (`SecaoPainel`/`SecaoMeuPerfil`, linhas ~30/38):
```tsx
const SecaoPainel: Secao = { label: 'Painel', href: '/painel', icone: 'home-outline' };
...
const SecaoMeuPerfil: Secao = { label: 'Meu perfil', href: '/perfil', icone: 'person-outline' };
```

**Depois**:
```tsx
const SecaoPainel: Secao = { label: 'Painel', href: '/painel', icone: 'home' };
...
const SecaoMeuPerfil: Secao = { label: 'Meu perfil', href: '/perfil', icone: 'perfil' };
```

**Antes** (`IconeDeSecao`, final do arquivo):
```tsx
function IconeDeSecao({ nome, ativo }: { nome: Secao['icone']; ativo: boolean }) {
  const escuro = useColorScheme() === 'dark';
  const paleta = escuro ? Colors.dark : Colors.light;
  const cor = ativo ? paleta.primary : paleta.text;
  return <Ionicons testID={`icone-secao-${nome}`} name={nome} size={18} color={cor} />;
}
```

**Depois**:
```tsx
const ComponentesPorIcone: Record<Secao['icone'], typeof House> = {
  home: House,
  perfil: User,
  'adicionar-aluno': UserPlus,
  chamada: ClipboardText,
  horarios: Clock,
  alunos: Users,
  alocacoes: LinkSimple,
  'valor-devido': CurrencyDollar,
  'entrar-turma': SignIn,
  'minhas-aulas': CalendarBlank,
  historico: ChartBar,
};

/**
 * Ícone Phosphor do item de menu (issue #202) — peso `regular`, 20px
 * (inline com o rótulo do item, ver `docs/spec/design-system.md#ícones`).
 * `ComponentesPorIcone` mapeia a chave pura de `secoesPorPapel.ts` (sem
 * JSX, ver comentário daquele arquivo) para o componente que efetivamente
 * desenha o ícone — mantém `secoesPorPapel.ts` desacoplado de React Native.
 */
function IconeDeSecao({ nome, ativo }: { nome: Secao['icone']; ativo: boolean }) {
  const escuro = useColorScheme() === 'dark';
  const paleta = escuro ? Colors.dark : Colors.light;
  const cor = ativo ? paleta.primary : paleta.text;
  const Icone = ComponentesPorIcone[nome];
  return <Icone testID={`icone-secao-${nome}`} size={20} color={cor} weight="regular" />;
}
```

Edge point: `phosphor-react-native` aceita `testID` como prop nativa (passa
adiante para o `Svg` subjacente) — se a versão instalada não repassar,
envolver em `<View testID={...}>` como fallback (checar antes de assumir).

## `frontend/src/components/organisms/HorarioCard.tsx`

**Antes** (`CardCorpo`, botões "Chamada"/"Remover"):
```tsx
<Link href={`/professor/${professorId}/horarios/${horario.id}/chamada`} asChild>
  <Pressable accessibilityRole="button" className="items-center justify-center" style={AlvoDeToqueMinimo}>
    <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
      Chamada
    </Text>
  </Pressable>
</Link>
<Pressable accessibilityRole="button" onPress={() => onRemover(horario.id)} className="items-center justify-center" style={AlvoDeToqueMinimo}>
  <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
</Pressable>
```

**Depois** (adiciona ícone antes do texto, mesmo padrão `flex-row items-center gap-one`):
```tsx
<Link href={`/professor/${professorId}/horarios/${horario.id}/chamada`} asChild>
  <Pressable accessibilityRole="button" className="flex-row items-center justify-center gap-one" style={AlvoDeToqueMinimo}>
    <ClipboardText testID="icone-acao-chamada" size={20} weight="regular" color={corPrimaria} accessibilityElementsHidden importantForAccessibility="no" />
    <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
      Chamada
    </Text>
  </Pressable>
</Link>
<Pressable accessibilityRole="button" onPress={() => onRemover(horario.id)} className="flex-row items-center justify-center gap-one" style={AlvoDeToqueMinimo}>
  <Trash testID="icone-acao-remover" size={20} weight="regular" color={corErro} accessibilityElementsHidden importantForAccessibility="no" />
  <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
</Pressable>
```

`corPrimaria`/`corErro`: resolver como já faz `IconeDeSecao` em
`MenuNavegacao.tsx` (via `useColorScheme()` + `Colors.light`/`Colors.dark`
de `@/theme/tokens`) — não inventar uma segunda forma de resolver cor de
tema neste arquivo, replicar o padrão existente.

Mesmo padrão de ícone + `flex-row`/`gap-one` para o botão "Cancelar" do
bloco de edição inline (adicionar `X` antes do texto "Cancelar", cor
`text-secondary`/`corSecundaria` igual à cor atual do texto).

## `frontend/src/components/atoms/Button.tsx`

**Antes** (assinatura e corpo, arquivo inteiro já lido em `task.md`):
```tsx
export type ButtonProps = PressableProps & {
  label: string;
  variante?: 'primario' | 'secundario';
};
...
export function Button({ label, disabled, variante = 'primario', ...pressableProps }: ButtonProps) {
  return (
    <Pressable ...>
      <Text ...>{label}</Text>
    </Pressable>
  );
}
```

**Depois**:
```tsx
import type { Icon as PhosphorIcon } from 'phosphor-react-native';

export type ButtonProps = PressableProps & {
  label: string;
  variante?: 'primario' | 'secundario';
  /** Ícone opcional (Phosphor), renderizado antes do label — ex:
   *  `icone={CalendarPlus}` em `HorarioVagoCard`. Sem esta prop, o botão
   *  renderiza exatamente como antes (nenhuma tela existente muda). */
  icone?: PhosphorIcon;
};

export function Button({ label, disabled, variante = 'primario', icone: Icone, ...pressableProps }: ButtonProps) {
  const corDoTexto = variante === 'secundario' ? 'text-text dark:text-dark-text' : 'text-white';
  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      className={`flex-row items-center justify-center gap-one border-2 px-four py-three active:opacity-80 ${ClassesPorVariante[variante]} ${
        disabled ? 'opacity-40' : ''
      }`}
      {...pressableProps}
    >
      {Icone ? (
        <Icone
          size={20}
          weight="regular"
          color={variante === 'secundario' ? corTextoTema : '#FFFFFF'}
          accessibilityElementsHidden
          importantForAccessibility="no"
        />
      ) : null}
      <Text className={`text-base font-semibold ${corDoTexto}`}>{label}</Text>
    </Pressable>
  );
}
```

Edge point: cor do ícone no `variante="primario"` usa branco fixo porque o
`Text` já usa `text-white` fixo nesse caso hoje (não é uma cor nova
introduzida por esta Task, é o padrão já existente do componente) —
`corTextoTema` para o caso `secundario` resolve via `Colors.light/dark` do
tema, mesmo padrão de `MenuNavegacao`/`HorarioCard` acima.

## `frontend/src/components/organisms/HorarioVagoCard.tsx`

**Antes**:
```tsx
<Button label="Marcar" onPress={() => onMarcar(horarioVago.id)} />
```

**Depois**:
```tsx
<Button label="Marcar" icone={CalendarPlus} onPress={() => onMarcar(horarioVago.id)} />
```
(import `CalendarPlus` de `phosphor-react-native`)

## `frontend/src/components/organisms/AulaProximaCard.tsx`

**Antes**:
```tsx
<Button label="Confirmar presença" ... />
<Button label="Cancelar" ... />
```

**Depois**:
```tsx
<Button label="Confirmar presença" icone={CheckCircle} ... />
<Button label="Cancelar" icone={X} ... />
```
(import `CheckCircle, X` de `phosphor-react-native`)

## `BotaoLoginGoogle.tsx` (RN #2 do card): exceção documentada

Decisão: **manter as cores atuais** (`#747775`, `#8E918F`, `#131314`,
`#1E1F20`, `#1F1F1F`, `#E3E3E3`, `bg-white`) — não adaptar à paleta do app.
Motivo: o botão "Sign in with Google" segue diretriz de marca de terceiro
(Google Identity Services), que especifica cores exatas por contrato de uso
da marca; adaptar a paleta quebraria a conformidade da marca sem ganho de
consistência real para o usuário (é o único botão de login de terceiro no
app, não estabelece precedente para nenhuma outra tela). Isso é a mesma
lógica já registrada no JSDoc do componente ("seguindo as diretrizes de
marca do Google"), só faltava a nota explícita perto do valor de cor em si
e o registro no `design-system.md`.

**Antes** (linha 88, sem comentário adjacente ao valor de cor):
```tsx
className={`w-full flex-row items-center justify-center gap-three border border-[#747775] bg-white px-four py-three active:bg-[#F8F9FA] dark:border-[#8E918F] dark:bg-[#131314] dark:active:bg-[#1E1F20] ${
  carregando ? 'opacity-60' : ''
}`}
```

**Depois** (mesmo valor, comentário acima citando a exceção):
```tsx
// Exceção documentada ao guardrail "sem fundo branco puro" e à paleta de
// `theme/palette.js` (ver docs/spec/design-system.md#cor, nota sobre
// BotaoLoginGoogle) — cores exatas exigidas pela diretriz de marca do
// Google para o botão "Sign in with Google" (issue #202).
className={`w-full flex-row items-center justify-center gap-three border border-[#747775] bg-white px-four py-three active:bg-[#F8F9FA] dark:border-[#8E918F] dark:bg-[#131314] dark:active:bg-[#1E1F20] ${
  carregando ? 'opacity-60' : ''
}`}
```

E acrescentar ao final da seção `## Cor` de `docs/spec/design-system.md`:
```markdown
**Exceção registrada**: `BotaoLoginGoogle.tsx` usa cores hex fixas fora
desta paleta (incluindo `bg-white`, que violaria o guardrail de fundo
branco puro) porque replica o botão oficial "Sign in with Google", cuja
marca exige cores exatas — decisão tomada na issue #202, não repetir esse
padrão em nenhum outro componente.
```

## `docs/spec/design-system.md#ícones`

Trocar o parágrafo "Recomendo Phosphor..." (ainda no futuro/condicional) por
uma confirmação de que já está em uso, mantendo as regras de peso/tamanho
como estão (nenhuma mudou nesta Task) e citando que hoje só o tamanho 20px
está em uso (nenhum botão-somente-ícone criado ainda).

## Dependência de outras Tasks

Nenhuma. Task isolada, sem migration, sem contrato de API novo.
