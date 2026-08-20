# Task: Professor obtém um código de convite curto além do link (#62)

Card: https://github.com/RafaVargas1/Synclass/issues/62

## Ordem de execução

- [x] Teste unidade (Domain): `Convite.Gerar` recebe também um código de 5
      dígitos e o expõe em `Codigo`.
- [x] Teste unidade (Domain): `ConviteService.GerarAsync` pede um código ao
      novo gerador e persiste junto com o convite.
- [x] Teste unidade (Domain): colisão de código contra convite ainda válido
      (não usado, não expirado) → gerador é chamado de novo até achar um
      código livre.
- [x] Teste unidade (Domain): código de um convite já usado ou expirado pode
      ser reaproveitado por um novo convite (checagem de unicidade ignora
      convites finalizados).
- [x] `GeradorDeCodigoConvite` (Infrastructure) + `IGeradorDeCodigoConvite`
      (Domain): gera 5 dígitos numéricos, aleatoriedade criptográfica (mesmo
      padrão de `GeradorDeCodigoOtp`/`GeradorDeTokenConvite`).
- [x] `IConviteRepository.ExisteCodigoAtivoAsync(codigo)`: novo método,
      implementado em `ConviteRepository` (EF Core) e em
      `FakeConviteRepository` (testes).
- [x] Migration: coluna `Codigo` (string, 5 chars) em `Convites`, índice não
      único (unicidade é só entre convites ativos, validada em domínio/serviço,
      não no banco).
- [x] Log estruturado: `ConviteGerado` passa a incluir se houve colisão/retry
      (quantidade de tentativas).
- [x] Teste de fumaça (Api): `POST /professores/{professorId}/convites` —
      resposta inclui `codigo` de 5 dígitos.
- [x] Frontend: `lib/api/convites.ts` (`GerarConviteResultado`) ganha
      `codigo`.
- [x] Frontend: `ConviteGerado.tsx` exibe o código ao lado do link, com teste
      de componente cobrindo o novo prop.
- [x] Frontend: `novo.tsx` passa `codigo` para `ConviteGerado`.
