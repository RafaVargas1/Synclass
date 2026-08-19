# Task: Usuário edita seu nome no perfil (#27)

Card: https://github.com/RafaVargas1/Synclass/issues/27

## Ordem de execução

- [x] Teste unidade (Domain): `Usuario.AtualizarNome` com nome válido troca `Nome`
- [x] Implementação mínima: `Usuario.AtualizarNome`
- [x] Teste unidade (Domain): `Usuario.AtualizarNome` com nome vazio/só espaços lança `NomeInvalidoException`
- [x] Implementação mínima: validação via `NomeUsuario.Validar` dentro de `AtualizarNome`
- [x] Teste unidade (Domain): `AtualizacaoNomeUsuarioService` atualiza o nome de um usuário existente e persiste
- [x] Teste unidade (Domain): `AtualizacaoNomeUsuarioService` com `usuarioId` inexistente lança `UsuarioNaoEncontradoException`
- [x] Implementação mínima: `AtualizacaoNomeUsuarioService`, `IUsuarioRepository.BuscarPorIdAsync`, `UsuarioRepository.BuscarPorIdAsync` (EF Core)
- [x] Teste de fumaça (Api): `PUT /usuarios/me/nome` autenticado com nome válido retorna 200 e nome atualizado
- [x] Teste de fumaça (Api): `PUT /usuarios/me/nome` com nome vazio retorna 400
- [x] Teste de fumaça (Api): `PUT /usuarios/me/nome` sem token retorna 401
- [x] Implementação: `UsuariosController.AtualizarNome`, log estruturado `NomeAtualizado`
- [x] Teste de fumaça (Api): `GET /usuarios/me` autenticado retorna nome atual
- [x] Implementação: `UsuariosController.Me`
- [x] Teste de fumaça (Api): `GET /professores/verificar-contato?contato=` com contato já cadastrado retorna `identidadeExistente=true` e o nome já salvo; com contato novo retorna `identidadeExistente=false`
- [x] Implementação: `ProfessoresController.VerificarContato` (reaproveita `IUsuarioRepository.BuscarPorContatoAsync`)
- [x] Componente frontend: `lib/api/usuarios.ts` (`buscarPerfil`, `atualizarNome`) contra o contrato já estabilizado
- [x] Componente frontend: `app/perfil.tsx` (tela de perfil, campo nome editável, guarda de rota igual `app/painel/index.tsx`)
- [x] Componente frontend: `lib/api/professores.ts` (`verificarContatoProfessor`)
- [x] Componente frontend: `CadastroProfessorForm` com prop `nomeReadonly`/texto de orientação
- [x] Componente frontend: `app/professor/cadastro.tsx` chama `verificarContatoProfessor` no blur do campo contato
- [x] Link de navegação para `/perfil` a partir de `app/painel/index.tsx`
