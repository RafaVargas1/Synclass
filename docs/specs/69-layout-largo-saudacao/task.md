# Task: Preencher melhor o layout em telas largas (web) (#69)

Card: https://github.com/RafaVargas1/Synclass/issues/69

## Ordem de execução

- [x] Teste unidade: `lib/periodoDoDia.ts` (`periodoDoDia(hora: number)`) —
      `0-11` retorna `'manha'`, `12-17` retorna `'tarde'`, `18-23` retorna
      `'noite'` (cobrir os limites 0, 11, 12, 17, 18, 23).
- [x] Implementação mínima: `lib/periodoDoDia.ts`, exporta `periodoDoDia` e
      `saudacaoPorPeriodo: Record<'manha' | 'tarde' | 'noite', string>`
      (`'Bom dia'` / `'Boa tarde'` / `'Boa noite'`).
- [x] Teste unidade: `app/painel/index.tsx` — generaliza
      `useUsuarioIdLogado` (renomeia para `usePerfilLogado`) pra buscar
      `buscarPerfil()` sempre que houver `token` (não mais só quando
      `papelAtivo === 'Professor'`), expondo `usuarioId` (continua só
      relevante pro Professor) **e** `nome`. Teste cobre: Aluno logado
      também recebe `nome` preenchido (hoje só Professor buscava);
      Professor continua recebendo `usuarioId` como antes; erro de rede
      continua marcando `erro`/`tentarNovamente` do jeito que já existe.
- [x] Implementação mínima do item acima.
- [x] Teste de componente: `PainelScreen` exibe `"{Saudação}, {nome}"` no
      topo (acima de `AlternadorDePapel`) usando `periodoDoDia(new
      Date().getHours())` — mock de `Date` pra cobrir os 3 períodos (ou
      mock direto de `periodoDoDia`, o que for mais simples de manter).
- [x] Implementação mínima: adiciona o texto de saudação em
      `PainelScreen`, usando `Heading` (ou `Text` com o mesmo estilo de
      título já usado nas outras telas) — sem novo componente/molecule
      dedicado, é um `Heading` + `periodoDoDia`.
- [x] Implementação mínima: `theme/tokens.ts` ganha
      `MaxContentWidthPainel = 1120` (nome explícito — não é o
      `MaxContentWidth` genérico de formulário); `PainelScreen` passa a
      usar esse valor em vez de `MaxContentWidth` no `style={{ maxWidth
      }}` do container.
- [ ] Docs: `docs/spec/design-system.md` — nova entrada na tabela de
      tokens de layout documentando `MaxContentWidthPainel` (valor, quando
      usar) ao lado de `MaxContentWidth`.
