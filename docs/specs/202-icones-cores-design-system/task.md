# Task: Padronizar ícones e cores da interface, seguindo o design-system já documentado (#202)

Card: https://github.com/RafaVargas1/Synclass/issues/202 (Closes #202)

Ver `implementation.md` desta mesma pasta para o desenho técnico completo
(nomes de ícone exatos, trecho antes/depois de cada arquivo). Este arquivo só
ordena os commits.

## Ordem de execução

- [ ] `npx expo install phosphor-react-native` (a partir de `frontend/`) — instala a versão compatível com Expo SDK 57. Remover `@expo/vector-icons` do `package.json` (único uso era `MenuNavegacao.tsx`, ver `implementation.md#dependência`).
- [ ] Teste (RNTL): atualizar `MenuNavegacao.test.tsx` — trocar os `testID` esperados de `icone-secao-home-outline`/`icone-secao-person-outline`/`icone-secao-person-add-outline` pelos novos nomes Phosphor (`icone-secao-home`, `icone-secao-perfil`, `icone-secao-adicionar-aluno`, ver tabela em `implementation.md`). Não alterar nenhuma outra asserção do arquivo (comportamento de destaque/rota/acessibilidade não muda).
- [ ] Implementação: `frontend/src/lib/secoesPorPapel.ts` — trocar o tipo `NomeIconeSecao` (união de nomes Ionicons) pela união de chaves Phosphor da tabela de `implementation.md`, e atualizar o campo `icone` de cada `Secao` (`secoesAluno`, `secoesProfessor`, `SecaoPainel`, `SecaoMeuPerfil` em `MenuNavegacao.tsx`) para a nova chave correspondente.
- [ ] Implementação: `frontend/src/components/organisms/MenuNavegacao.tsx` — remover o import de `Ionicons`, importar os componentes Phosphor usados (ver mapa `IconesDeSecao` em `implementation.md`), e reescrever `IconeDeSecao` para renderizar o componente Phosphor correspondente à chave, peso `regular`, tamanho `20` (ícone inline com o rótulo do item de menu), cor igual à lógica atual (`primary` quando ativo, `text` quando inativo). Manter o `testID={`icone-secao-${nome}`}` e o padrão de rótulo acessível já existente (o `accessibilityLabel` fica no `Link` pai, não no ícone).
- [ ] Rodar a suíte escopada (`npx jest MenuNavegacao`) e confirmar verde antes de seguir.
- [ ] Documentação: `docs/spec/design-system.md#ícones` — trocar a menção "recomendo Phosphor" (ainda não implementada) por confirmação de que o menu já usa Phosphor, citando os dois tamanhos realmente usados (20px itens de menu, nenhum uso de 24px ainda nesta Task).
- [ ] Decisão de cor em `BotaoLoginGoogle.tsx` (RN #2 do card): aceitar como exceção documentada — Google exige cores exatas para o botão "Sign in with Google" (diretriz de marca de terceiro, fora do controle do design-system do Synclass). Adicionar comentário explícito imediatamente acima da `className` com os hex (`#747775`, `#8E918F`, `#131314`, `#1E1F20`, `#1F1F1F`, `#E3E3E3`, `bg-white`) citando essa decisão, e acrescentar um parágrafo curto em `docs/spec/design-system.md` (seção `## Cor`, ao final) documentando a exceção. Nenhum valor de cor muda no componente.
- [ ] Teste (RNTL): `HorarioCard.test.tsx` — adicionar asserção de que o botão "Chamada" e o botão "Remover" (dentro de `CardCorpo`) cada um contém um ícone com `testID` próprio (`icone-acao-chamada`, `icone-acao-remover`), sem alterar o texto visível nem o `accessibilityLabel` (o rótulo acessível continua vindo do texto, não do ícone).
- [ ] Implementação: `frontend/src/components/organisms/HorarioCard.tsx` — em `CardCorpo`, adicionar `ClipboardText` (mesma família do ícone "Fazer chamada" do menu, consistência entre menu e ação de tela) antes do texto "Chamada", e `Trash` antes do texto "Remover". Tamanho 20 (inline com texto do botão), peso `regular`, cor herdada da mesma classe de texto do botão (`primary`/`error`). Ícone marcado `accessibilityElementsHidden`/`importantForAccessibility="no"` (o texto já é o nome acessível).
- [ ] Teste (RNTL): `HorarioCard.test.tsx` — no bloco de edição inline, adicionar asserção de que o botão "Cancelar" (descartar edição) contém um ícone `X` com `testID` `icone-acao-cancelar` — mesmo ícone que será usado em `AulaProximaCard` para o botão "Cancelar" (critério de aceite 3: mesma ação, mesmo glifo em toda tela).
- [ ] Implementação: `frontend/src/components/organisms/HorarioCard.tsx` — adicionar `X` antes do texto "Cancelar" no bloco de edição inline, mesmo padrão de tamanho/peso/acessibilidade do item anterior.
- [ ] Teste (RNTL): `HorarioVagoCard.test.tsx` — adicionar asserção de que o `Button` "Marcar" recebe um ícone `CalendarPlus` (via nova prop `icone` de `Button.tsx`, ver `implementation.md#contrato-de-button`) com `testID` `icone-acao-marcar`.
- [ ] Implementação: `frontend/src/components/atoms/Button.tsx` — adicionar prop opcional `icone?: React.ComponentType<IconProps>` que, quando presente, renderiza o ícone (peso `regular`, tamanho 20, cor igual ao texto do botão) antes do `Text`, com `accessibilityElementsHidden`/`importantForAccessibility="no"`. Sem a prop, o `Button` continua exatamente como hoje (nenhuma tela existente muda visualmente).
- [ ] Implementação: `frontend/src/components/organisms/HorarioVagoCard.tsx` — passar `icone={CalendarPlus}` ao `Button` "Marcar".
- [ ] Teste (RNTL): `AulaProximaCard.test.tsx` — adicionar asserção de que o `Button` "Cancelar" usa o ícone `X` (`testID` `icone-acao-cancelar`, mesma chave usada em `HorarioCard`) e o `Button` "Confirmar presença" usa `CheckCircle` (`testID` `icone-acao-confirmar-presenca`).
- [ ] Implementação: `frontend/src/components/organisms/AulaProximaCard.tsx` — passar `icone={X}` ao `Button` "Cancelar" e `icone={CheckCircle}` ao `Button` "Confirmar presença".
- [ ] Rodar suíte escopada dos 4 componentes tocados (`npx jest MenuNavegacao HorarioCard HorarioVagoCard AulaProximaCard BotaoLoginGoogle`) e confirmar verde.
- [ ] Ajuste de config, só se necessário: se `npx jest` falhar por `phosphor-react-native` não transpilado (erro de sintaxe ESM em `node_modules`), adicionar `phosphor-react-native` ao `transformIgnorePatterns` do preset `jest-expo` em `frontend/package.json` (bloco `"jest"`), seguindo a mesma forma usada por outros pacotes Expo nesse preset — não inventar um transform novo.
- [ ] Gate completo antes de commit final: `npm run lint && npm run typecheck && npm test` (suíte completa, não só os arquivos tocados).

## Fora de escopo (não implementar nesta Task)

- Ícone no botão "Salvar"/"Editar política" de `HorarioCard` (rótulo não citado pelo card #202).
- Substituir o hambúrguer customizado (`IconeHamburguer`, três barras desenhadas) por um ícone Phosphor — o card pede a troca de biblioteca só onde `@expo/vector-icons` já era usado (`IconeDeSecao`); o hambúrguer nunca usou biblioteca de ícone, é forma customizada, e não está listado nos 3 desvios do card.
- Qualquer mudança de cor em `BotaoLoginGoogle.tsx` além do comentário de documentação da exceção.
