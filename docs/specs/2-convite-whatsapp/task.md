# Task: Professor convida Aluno via WhatsApp e Aluno completa cadastro (#2)

Card: https://github.com/RafaVargas1/Synclass/issues/2

## Ordem de execução

### Domain

- [x] Teste unidade (Domain): `Contato.IdentificarTipo` classifica e-mail e
      telefone normalizados (edge point de apoio à coluna `ContatoTipo`)
- [x] Teste unidade (Domain): `Convite.Gerar` cria convite não expirado com
      token de alta entropia e `ExpiraEm` = agora + N dias configurável
      (critério de aceite 1)
- [x] Teste unidade (Domain): `Convite.MarcarUsado` rejeita convite já usado
      com `ConviteInvalidoException` (edge point — uso único)
- [x] Teste unidade (Domain): `Convite.MarcarUsado` rejeita convite expirado
      com `ConviteExpiradoException` (critério de aceite 3)
- [x] Implementação mínima: `Convite`, `IConviteRepository`,
      `IGeradorDeTokenConvite`, exceções (`ConviteRejeitadoException` e
      subtipos)
- [x] Teste unidade (Domain): `ConviteService.GerarAsync` — contato válido
      sem Aluno prévio cria convite vinculado ao Professor (critério de
      aceite 1)
- [x] Teste unidade (Domain): `ConviteService.GerarAsync` — `professorId`
      inexistente rejeita com `ProfessorNaoEncontradoException`
- [x] Teste unidade (Domain): `ConviteService.GerarAsync` — `matriculaId` de
      origem inexistente, de outro Professor, ou já promovida rejeita com
      `MatriculaOrigemInvalidaException` (edge point)
- [x] Teste unidade (Domain): `ConviteService.GerarAsync` — contato já
      vinculado como Aluno pleno a este Professor rejeita com
      `ContatoJaVinculadoException` (critério de aceite 4)
- [x] Implementação mínima: `ConviteService.GerarAsync`
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — token
      inexistente rejeita com `ConviteInvalidoException`
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — contato
      divergente do convite rejeita com `ConviteContatoDivergenteException`
      (edge point)
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — sem
      `MatriculaId` de origem e contato novo cria `Usuario` com papel Aluno e
      uma `Matricula` vinculada nova (critério de aceite 2, caminho "contato
      novo")
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — contato já
      existente como Aluno provisório de outro convite/fluxo promove a
      identidade existente em vez de duplicar (critério de aceite 2, caminho
      "identidade existente")
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — com
      `MatriculaId` de origem promove exatamente aquela `Matricula`
      provisória (sem casar por nome/contato), preservando `MatriculaId`
      (critério de aceite 5)
- [x] Teste unidade (Domain): `ConviteService.AceitarAsync` — convite
      expirado rejeita sem criar/alterar nada (critério de aceite 3)
- [x] Implementação mínima: `ConviteService.AceitarAsync`

### Infrastructure

- [x] Migration (`dotnet ef migrations add CriaConvite`): tabela `Convites`
      (`Id`, `ProfessorId` FK → `Usuarios`, `Contato`, `ContatoTipo`,
      `Token` com índice único, `MatriculaId` FK nullable → `Matriculas`,
      `ExpiraEm`, `UsadoEm` nullable, `CreatedAt`); `ConviteConfiguration`;
      `ConviteRepository` (EF Core)
- [x] Implementação: `GeradorDeTokenConvite` (`RandomNumberGenerator`,
      string base64url de alta entropia, não sequencial)
- [x] Estende `IMatriculaRepository`/`MatriculaRepository` com
      `BuscarPorIdAsync` e `BuscarVinculoAsync` (Professor + AlunoUsuarioId),
      necessários para o aceite/checagem de vínculo prévio

### Api

- [x] Teste de fumaça (Api): `POST /professores/{professorId}/convites` com
      contato válido retorna 200 com `token`/`expiraEm`
- [x] Teste de fumaça (Api): `POST /professores/{professorId}/convites` com
      `professorId` inexistente retorna 404
- [x] Teste de fumaça (Api): `POST /professores/{professorId}/convites` para
      Aluno já vinculado retorna 400
- [x] Teste de fumaça (Api): `POST /convites/{token}/aceite` com token válido
      e contato correspondente retorna 200 com `usuarioId`
- [x] Teste de fumaça (Api): `POST /convites/{token}/aceite` com token
      expirado retorna 400 com mensagem de expiração
- [x] Implementação mínima: `ConvitesController`, DTOs, registro de DI
      (`Program.cs`, incluindo `Convites:DiasValidade` configurável),
      logs estruturados `ConviteGerado` (Information), `ConviteAceito`
      (Information), `ConviteRejeitado` (Warning) — mesma convenção de
      `AutenticacaoController`/`AlunosProvisoriosController`

### Frontend

- [x] Teste (`lib/api/convites.ts`): `gerarConvite` e `aceitarConvite`
      contra o contrato já estabilizado (sucesso, erro de negócio, erro de
      conexão/timeout)
- [x] Implementação: `frontend/src/lib/api/convites.ts`
- [ ] Teste (`lib/whatsapp.ts`): `montarLinkWhatsApp` gera link `wa.me` com
      número BR quando o contato é telefone, e link genérico (sem
      destinatário) quando é e-mail
- [ ] Implementação: `frontend/src/lib/whatsapp.ts`
- [ ] Teste de tela: `frontend/src/app/professor/[professorId]/convites/novo.tsx`
      (gerar convite, exibir link e botão wa.me, erro exibido)
- [ ] Implementação: tela de geração de convite
- [ ] Teste de tela: `frontend/src/app/convite/[token].tsx` (reaproveita
      `CadastroProfessorForm`; sucesso mostra confirmação, expirado mostra
      mensagem + ação de pedir novo link)
- [ ] Implementação: rota pública de aceite

A ordem segue backend até o contrato da Api estabilizar, depois frontend
(ver `fluxo-de-feature.md#fase-3--implementação`).
