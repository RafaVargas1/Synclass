# Implementação: Home/Login — hierarquia, rótulos, Google, fricção

## Componentes afetados

- **Novo** `frontend/src/components/atoms/IconeGoogle.tsx`: SVG inline
  (via `react-native-svg`, já usado no projeto? confirmar dependência
  antes — se não houver, usar `Image`/`View` com paths de cor sólida
  simplificados, mas priorizar `react-native-svg` se já for dependência
  do Expo/projeto, é o padrão pra ícone vetorial em RN).
- **Modificado** `frontend/src/components/molecules/BotaoLoginGoogle.tsx`:
  troca o átomo `Button` genérico por um botão próprio com fundo claro,
  borda, `IconeGoogle`. Mesma API pública (`onAutenticado`,
  `onCadastroPendente`) — os 3 chamadores atuais (`login/index.tsx`,
  `professor/cadastro.tsx`, `aluno/index.tsx`) não mudam.
- **Modificado** `frontend/src/app/login/index.tsx`: gap/divisor entre
  `SolicitarCodigoForm` e `BotaoLoginGoogle`; aceita `?email=` via
  `useLocalSearchParams` pra inicializar `emailPendente` (chegando já no
  estado de escolha Professor/Aluno do cadastro pendente).
- **Modificado** `frontend/src/components/organisms/HomeHero.tsx`: ganha
  `onAutenticadoGoogle`/`onCadastroPendenteGoogle`, renderiza
  `BotaoLoginGoogle` como primeiro/destacado; CTAs de cadastro renomeados
  e com estilo secundário (borda, não preenchimento).
- **Modificado** `frontend/src/components/templates/HomeTemplate.tsx`:
  repassa as duas props novas.
- **Modificado** `frontend/src/app/index.tsx`: ganha a mesma wiring de
  sessão que `login/index.tsx` já tem pro Google (definir sessão +
  navegar, ou navegar pro cadastro pendente levando o e-mail).

## Contrato de API

Nenhum — mudança puramente client-side, reaproveitando os endpoints de
login/Google já existentes (`loginComGoogle`, `obterIdTokenGoogle`).

## Decisões de design

### Por que Google fica embutido na Home em vez de a Home só linkar pra `/login`

Issue #114 pede reduzir fricção do caminho mais frequente (usuário
recorrente). Login por código genuinamente precisa de uma tela própria
(dois passos: contato → código recebido), mas Google é autenticação de
um toque — colocá-lo atrás de uma navegação extra só pra "abrir a tela
que tem o botão" não tem função, dado que o SDK do Google já pode ser
acionado direto da Home.

### Por que reaproveitar `/login?email=` para o cadastro pendente do Google-na-Home

`login/index.tsx` já tem a UI completa de "e-mail sem conta → escolha
Professor/Aluno" (branch `emailPendente`). Duplicar essa UI dentro da
Home custaria mais manutenção (dois lugares pra manter a mesma escolha)
do que aceitar `email` via query param e inicializar o mesmo estado.
Mesmo padrão que `professor/cadastro.tsx`/`aluno/index.tsx` já usam pra
pré-preencher contato vindo do Google.

### Hierarquia visual da Home

- 1º: `BotaoLoginGoogle` (preenchido/destacado, dentro do estilo próprio
  de botão Google definido pela issue #113 — não o `Button` azul do app).
- 2º: "Entrar com código" — link secundário (texto, sem preenchimento),
  mesmo tratamento que hoje.
- 3º: "Cadastrar como Professor" / "Cadastrar como Aluno" — botões
  secundários (borda, sem preenchimento sólido), abaixo dos dois
  caminhos de entrada.

## Testes

Cobertura nova em `HomeHero.test.tsx` (rótulos, ordem/hierarquia,
callbacks do Google repassados), `BotaoLoginGoogle.test.tsx` (ícone +
estilo), `login/index.test.tsx` (divisor/gap, `?email=` pré-preenche
`emailPendente`), e novo `app/index.test.tsx` (`HomeScreen`) cobrindo a
wiring de sessão que antes só existia em `login/index.tsx`.
