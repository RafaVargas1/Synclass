# Task: Aluno usa o código de convite para entrar na turma do Professor (#63)

Card: https://github.com/RafaVargas1/Synclass/issues/63

## Ordem de execução

- [x] `IConviteRepository`: adiciona `BuscarPorCodigoAsync(codigo, ct)` (mais
      recente por `CreatedAt` entre convites com aquele código — um código
      finalizado pode ter sido reaproveitado por um convite mais novo, ver
      `implementation.md#edge-points`), implementa em `ConviteRepository`
      (EF Core) e em `FakeConviteRepository` (testes).
- [x] `ConviteService`: extrai `AceitarResolvidoAsync(convite, nome,
      contatoBruto, rejeitarVinculoExistente, ct)` do corpo atual de
      `AceitarAsync`, sem mudar nenhum teste existente de `AceitarAsync`
      (refactor puro).
- [x] Teste unidade (Domain): código válido e não usado, Aluno já
      autenticado (identidade já existente com papel Aluno) → cria vínculo
      com o Professor, convite marcado como usado.
- [x] `ConviteService.AceitarPorCodigoAsync(codigoBruto, nome, contatoBruto,
      ct)`: normaliza o código (só dígitos), busca via
      `BuscarPorCodigoAsync`, delega a `AceitarResolvidoAsync` com
      `rejeitarVinculoExistente: true`.
- [x] Teste unidade (Domain): código expirado → `ConviteExpiradoException`
      (mesma mensagem do fluxo por link).
- [x] Teste unidade (Domain): código já usado ou inexistente →
      `ConviteInvalidoException`.
- [x] Teste unidade (Domain): Aluno sem conta que informa código válido +
      nome/contato → cria usuário com papel Aluno já vinculado ao Professor
      do convite.
- [x] Teste unidade (Domain): contato já existe como Professor → papel
      Aluno é adicionado à mesma conta (reaproveita
      `AdicionarPapelAlunoIdempotente`, já coberto indiretamente, mas cubra
      o caminho via código explicitamente).
- [x] Teste unidade (Domain): Aluno que já tem vínculo com aquele Professor
      informa o código de novo → `ContatoJaVinculadoException` (reaproveita
      a exception já usada em `GerarAsync`), convite **não** é marcado como
      usado (mesma garantia de "rejeição não muda nada" de
      `ObterMatriculaOrigemValidaAsync`).
- [x] Teste unidade (Domain): normalização do código aceita espaços/máscara
      (ex: "12 345" ou "1-2-3-4-5") equivalente a "12345".
- [x] `AceitarConviteRequest` (Api): reaproveitado tal como está (mesmo
      formato `Nome`/`Contato`).
- [x] `ConvitesController`: `POST /convites/codigo/{codigo}/aceite`
      (`AllowAnonymous`), espelhando `Aceitar(token, ...)` — mesmo
      tratamento de exceções, mesmo log `ConviteAceito`/`PapelAdicionado`/
      `ConviteRejeitado`.
- [x] Teste de fumaça (Api): `POST /convites/codigo/{codigo}/aceite` —
      sucesso (Aluno novo), código expirado, código inválido/usado, contato
      já vinculado (mesmos quatro cenários do card).
- [x] Frontend: `lib/api/convites.ts` — `aceitarConvitePorCodigo(input)`,
      mesmo formato de `aceitarConvite`, apontando para a rota nova.
- [ ] Frontend: tela `app/aluno/entrar-turma.tsx` — campo de código (5
      dígitos) + `CadastroUsuarioForm` (nome/contato), reaproveitando
      `AceiteConviteConfirmado`/`ConviteExpirado` do fluxo por link. Teste
      de tela cobrindo os cenários do card (sucesso, expirado, inválido,
      contato já vinculado).
- [ ] Edge point: normaliza o código no frontend antes de enviar (mesmo
      guardrail do backend, mensagem consistente).
