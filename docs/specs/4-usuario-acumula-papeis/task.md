# Task: Usuário pode acumular papéis de Professor e Aluno (#4)

Card: https://github.com/RafaVargas1/Synclass/issues/4

## Ordem de execução

### Domain

- [ ] Confirmar (sem novo teste): `Usuario.AdicionarPapel` e sua cobertura de
      idempotência/duplicidade já existem (`UsuarioTests.cs`,
      `AdicionarPapel_PapelDiferenteDoExistente_AdicionaSemDuplicarIdentidade`
      e `AdicionarPapel_PapelJaAtribuido_LancaPapelJaAtribuidoException`) —
      nada novo aqui, issue #1 já entregou.
- [ ] Teste unidade: `CadastroProfessorService.CadastrarProfessorAsync`
      devolve indicador `UsuarioReaproveitado = true` quando adiciona o
      papel Professor a um usuário existente (ex: já Aluno); `false` quando
      cria usuário novo.
- [ ] Implementação mínima: `CadastroProfessorService` retorna
      `ResultadoCadastroProfessor(Usuario, bool UsuarioReaproveitado)`.
- [ ] Teste unidade: `ConviteService.AceitarAsync` devolve indicador
      `PapelAdicionado = true` quando o papel Aluno é de fato anexado a um
      usuário existente; `false` quando o usuário já tinha o papel
      (idempotente, sem-op) ou é recém-criado.
- [ ] Implementação mínima: `ResultadoAceiteConvite` ganha o campo
      `PapelAdicionado`.

### Infrastructure/Api — autorização por papel

- [ ] Teste de fumaça (Api): `POST /professores/{id}/horarios` (endpoint
      Professor-only já existente) retorna 401 sem header `Authorization`.
- [ ] Teste de fumaça (Api): mesmo endpoint retorna 403 com token válido mas
      sem o papel `Professor` (ex: token só com `Aluno`).
- [ ] Teste de fumaça (Api): mesmo endpoint aceita a requisição (não
      401/403) com token contendo o papel `Professor`.
- [ ] Implementação: `Program.cs` — `AddAuthentication().AddJwtBearer(...)`
      (mesma `Jwt:SigningKey` já usada por `GeradorDeTokenSessaoJwt`) +
      `AddAuthorization()` + `app.UseAuthentication()` antes de
      `app.UseAuthorization()` (já existente).
- [ ] Implementação: `[Authorize(Roles = "Professor")]` em
      `HorariosController`, `AlocacoesHorarioController`,
      `AlunosProvisoriosController`, `RegraDeCobrancaController`,
      `ConfiguracoesController` (nível de classe) e no método `Gerar` de
      `ConvitesController`; `[AllowAnonymous]` explícito no método `Aceite`
      do mesmo controller (convite é aceito por quem ainda não tem sessão).
- [ ] Teste de fumaça (Api): `POST /auth/confirmacao` — confirma (ou
      complementa, se faltar assert) que o corpo da resposta inclui
      `papeis` (já implementado na issue #18).
- [ ] Log estruturado: evento `PapelAdicionado` (Information, `TrackId`,
      `UsuarioId`, `Papel`) em `ProfessoresController` (quando
      `UsuarioReaproveitado == true`) e em `ConvitesController` (quando
      `PapelAdicionado == true`) — ver
      `architecture.md#logs-estruturados-e-track-id`.

### Frontend

- [ ] Teste (`lib/auth/sessao.ts`): salvar/ler/limpar papéis junto do token.
- [ ] Implementação: `sessao.ts` ganha `salvarPapeis`/`lerPapeis` (mesmo
      padrão de `salvarToken`/`lerToken`).
- [ ] Implementação: `app/login/verificar.tsx` persiste
      `resultado.papeis` (hoje descartado) e navega para `/painel` em vez
      de só mostrar confirmação (não há mais "sem área logada", ver
      `docs/specs/18-login-otp/implementation.md`).
- [ ] Teste (`lib/auth/contexto-sessao.tsx`): hook `useSessao` expõe
      `papeis`, `papelAtivo` (default: primeiro papel) e
      `definirPapelAtivo`.
- [ ] Implementação: `contexto-sessao.tsx` (Provider + hook).
- [ ] Teste (`components/organisms/AlternadorDePapel.tsx`): renderiza abas
      só quando `papeis.length > 1`; chama `definirPapelAtivo` ao trocar de
      aba; não renderiza nada com um único papel.
- [ ] Implementação: `AlternadorDePapel.tsx`.
- [ ] Teste de tela (`app/painel/index.tsx`): redireciona para `/login`
      sem sessão salva; mostra `AlternadorDePapel` só com múltiplos papéis;
      conteúdo (lista de ações) muda conforme `papelAtivo`.
- [ ] Implementação: `app/painel/index.tsx`.
