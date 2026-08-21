# Task: Alvo de toque ≥44×44pt em componentes compartilhados e ad-hoc (#115)

Card: https://github.com/RafaVargas1/Synclass/issues/115

Padrão de correção (já aplicado uma vez em `Topbar.tsx`, "Voltar" —
usar de referência): manter o traço/padding visual do desenho, e
garantir a área de toque via `style={{ minWidth: 44, minHeight: 44 }}`
no `Pressable`, com `items-center justify-center` na className pra
centralizar o conteúdo visual dentro dessa área maior. Teste: `toHaveStyle({ minWidth: 44, minHeight: 44 })` sobre o `Pressable`
(mesmo padrão já usado em `Topbar.test.tsx`, "gives the back button a
touch target of at least 44x44").

## Ordem de execução

- [x] Teste de componente: `ChipSelector` — cada chip (`Chip` interno) tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/components/molecules/ChipSelector.tsx` — `Chip` ganha `style={{ minWidth: 44, minHeight: 44 }}` e `items-center justify-center` na className, mantendo `px-two py-one rounded-small`
- [x] Teste de componente: `AlternadorDePapel` — cada aba (`Aba` interno) tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/components/organisms/AlternadorDePapel.tsx` — mesmo tratamento em `Aba`
- [x] Teste de componente: `professor/[professorId]/valor-devido.tsx` — cada opção do filtro de modo (`ChipDeModo` ou equivalente interno) tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: aplicar o mesmo padrão ao componente de filtro de período nessa tela (localizar o `Pressable` do chip "Todos"/"Este mês"/"Personalizado" e aplicar o mesmo tratamento — sem duplicar o `Chip`/`Aba` já existentes, reaproveitar `ChipSelector` se a estrutura permitir, senão aplicar o padrão localmente)
- [x] Teste de componente: `HorarioCard` — botões "Cancelar"/"Salvar"/"Editar política"/"Remover" têm `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/components/organisms/HorarioCard.tsx` — aplicar o padrão aos `Pressable` desses botões
- [x] Teste de componente: `HorarioAlocacaoCard` — botão "Remover" tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/components/organisms/HorarioAlocacaoCard.tsx` — mesmo tratamento
- [x] Teste de componente: `painel/index.tsx` — botão "Sair" (`BotaoSair`) tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/app/painel/index.tsx` — mesmo tratamento em `BotaoSair`
- [x] Teste de componente: `MenuNavegacao` — botão "Abrir menu"/"Fechar menu" (`BotaoAlternarMenu`) tem `minWidth`/`minHeight` de 44 efetivos
- [x] Implementação: `frontend/src/components/organisms/MenuNavegacao.tsx` — mesmo tratamento em `BotaoAlternarMenu`

### Inconsistências encontradas

_(Nenhuma até o momento — preencher durante a implementação se houver.)_
