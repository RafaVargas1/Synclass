# Task: Usuário edita seu nome no perfil (#27)

Card: https://github.com/RafaVargas1/Synclass/issues/27

## Ordem de execução

- [ ] Teste unidade (Domain): `Usuario.AtualizarNome` com nome válido troca `Nome`
- [ ] Implementação mínima: `Usuario.AtualizarNome`
- [ ] Teste unidade (Domain): `Usuario.AtualizarNome` com nome vazio/só espaços lança `NomeInvalidoException`
- [ ] Implementação mínima: validação via `NomeUsuario.Validar` dentro de `AtualizarNome`
- [ ] Teste unidade (Domain): `AtualizacaoNomeUsuarioService` atualiza o nome de um usuário existente e persiste
- [ ] Teste unidade (Domain): `AtualizacaoNomeUsuarioService` com `usuarioId` inexistente lança `UsuarioNaoEncontradoException`
- [ ] Implementação mínima: `AtualizacaoNomeUsuarioService`, `IUsuarioRepository.BuscarPorIdAsync`, `UsuarioRepository.BuscarPorIdAsync` (EF Core)
- [ ] Teste de fumaça (Api): `PUT /usuarios/me/nome` autenticado com nome válido retorna 200 e nome atualizado
- [ ] Teste de fumaça (Api): `PUT /usuarios/me/nome` com nome vazio retorna 400
- [ ] Teste de fumaça (Api): `PUT /usuarios/me/nome` sem token retorna 401
- [ ] Implementação: `UsuariosController.AtualizarNome`, log estruturado `NomeAtualizado`
- [ ] Teste de fumaça (Api): `GET /usuarios/me` autenticado retorna nome atual
- [ ] Implementação: `UsuariosController.Me`
- [ ] Teste de fumaça (Api): `GET /professores/verificar-contato?contato=` com contato já cadastrado retorna `identidadeExistente=true` e o nome já salvo; com contato novo retorna `identidadeExistente=false`
- [ ] Implementação: `ProfessoresController.VerificarContato` (reaproveita `IUsuarioRepository.BuscarPorContatoAsync`)
- [ ] Componente frontend: `lib/api/usuarios.ts` (`buscarPerfil`, `atualizarNome`) contra o contrato já estabilizado
- [ ] Componente frontend: `app/perfil.tsx` (tela de perfil, campo nome editável, guarda de rota igual `app/painel/index.tsx`)
- [ ] Componente frontend: `lib/api/professores.ts` (`verificarContatoProfessor`)
- [ ] Componente frontend: `CadastroProfessorForm` com prop `nomeReadonly`/texto de orientação
- [ ] Componente frontend: `app/professor/cadastro.tsx` chama `verificarContatoProfessor` no blur do campo contato
- [ ] Link de navegação para `/perfil` a partir de `app/painel/index.tsx`
