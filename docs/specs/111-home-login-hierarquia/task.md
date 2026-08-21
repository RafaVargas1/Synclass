# Task: Home/Login — hierarquia, rótulos, botão Google e fricção (#111, #112, #113, #114)

Cards:
- https://github.com/RafaVargas1/Synclass/issues/111 (hierarquia login vs. cadastro)
- https://github.com/RafaVargas1/Synclass/issues/112 (rótulos "sou X" não comunicam cadastro)
- https://github.com/RafaVargas1/Synclass/issues/113 (botão Google sem identidade oficial)
- https://github.com/RafaVargas1/Synclass/issues/114 (fricção: Google exige Home→Login)

As 4 issues tocam o mesmo fluxo (Home + Login) e se resolvem juntas num
desenho coerente: Google fica acessível direto na Home (resolve #114),
como o CTA mais destacado (resolve #111), com identidade visual oficial
(resolve #113); os botões de cadastro passam a rótulo de ação explícito e
peso visual secundário (resolve #111 + #112).

## Ordem de execução

- [x] Implementação: `frontend/src/components/atoms/IconeGoogle.tsx` — SVG inline do "G" multicolor oficial do Google (24×24, sem depender de asset externo)
- [x] Teste de componente: `BotaoLoginGoogle` — renderiza o `IconeGoogle` e o rótulo "Entrar com Google" com estilo de botão claro/borda (não mais o `Button` genérico azul sólido)
- [x] Implementação: `BotaoLoginGoogle.tsx` — troca o `Button` genérico por um botão próprio (fundo claro/branco, borda, `IconeGoogle` à esquerda do texto), mantendo a mesma API de props (`onAutenticado`/`onCadastroPendente`) e o estado "Entrando..." durante `carregando`
- [x] Teste de componente: `login/index.tsx` — `SolicitarCodigoForm` e `BotaoLoginGoogle` têm separação visual (divisor "ou" ou gap), não ficam colados
- [x] Implementação: `login/index.tsx` — adiciona gap/divisor "ou" entre os dois métodos de entrada; aceita `email` via `useLocalSearchParams` e pré-preenche `emailPendente` quando presente (permite chegar já no estado de "cadastro pendente" vindo da Home)
- [x] Teste de componente: `HomeHero` — rótulos de cadastro dizem "Cadastrar como Professor"/"Cadastrar como Aluno" (não mais "sou Professor"/"sou Aluno")
- [x] Teste de componente: `HomeHero` — `BotaoLoginGoogle` é o primeiro elemento / maior peso visual (botão preenchido/destacado), CTAs de cadastro vêm depois com estilo secundário (borda, não preenchimento sólido)
- [x] Teste de componente: `HomeHero` — link "Entrar com código" (renomeado de "Já tenho conta, entrar") continua presente como caminho alternativo pro login por código
- [x] Implementação: `HomeHero.tsx` — recebe `onAutenticadoGoogle`/`onCadastroPendenteGoogle` (repassados a um `BotaoLoginGoogle` embutido, primeiro/destacado), reordena e restila os CTAs de cadastro (secundário), renomeia rótulos
- [x] Implementação: `HomeTemplate.tsx` — repassa as duas novas props de `HomeScreen` para `HomeHero`
- [x] Teste de componente: `HomeScreen` (`app/index.test.tsx`, novo) — Google autenticado com sucesso define sessão e navega pra `/painel`; Google com cadastro pendente navega pra `/login?email=...`
- [x] Implementação: `frontend/src/app/index.tsx` — adiciona `useSessao().definirSessao`, `handleAutenticadoGoogle` (definirSessao + `router.replace('/painel')`) e `handleCadastroPendenteGoogle` (`router.push({pathname:'/login', params:{email}})`), passa pro `HomeTemplate`

### Inconsistências encontradas

_(Nenhuma até o momento — preencher durante a implementação se houver.)_
