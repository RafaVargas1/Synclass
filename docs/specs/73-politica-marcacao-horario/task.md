# Task: Horário ganha política de marcação própria (Livre/Fixo/Híbrido) no cadastro (#73)

Card: https://github.com/RafaVargas1/Synclass/issues/73

## Ordem de execução

- [ ] Teste unidade (Domain): `Horario.Criar` com cada uma das 3 políticas (`Livre`, `Fixo`, `Hibrido`) carrega essa política em `TipoMarcacao`.
- [ ] Teste unidade (Domain): `Horario.Criar` rejeita `TipoMarcacao` fora do enum com `TipoMarcacaoInvalidoException`.
- [ ] Implementação mínima: enum `TipoMarcacao` (Domain/Horarios), `TipoMarcacaoInvalidoException`, parâmetro obrigatório `tipoMarcacao` em `Horario.Criar` + propriedade `TipoMarcacao`.
- [ ] Ajuste `HorarioService.CadastrarAsync` para receber e repassar `tipoMarcacao` (obrigatório, sem default).
- [ ] Ajuste `HorariosController`/DTO de cadastro (`Synclass.Api`) para receber `tipoMarcacao` (int) no `POST /professores/{professorId}/horarios` e repassar ao service.
- [ ] Migration `AdicionaTipoMarcacaoHorario`: coluna `TipoMarcacao` (int, not null) na tabela `Horarios`.
- [ ] Ajuste dos testes de fumaça (Api) existentes em `HorarioEndpointTests` para passar `TipoMarcacao` (campo agora obrigatório no `CriarHorarioRequest`, quebra os call sites atuais).
- [ ] Teste de fumaça (Api): `POST /professores/{professorId}/horarios` — 200 com cada uma das 3 políticas válidas; 400 quando `TipoMarcacao` está fora do enum (mesmo padrão de `Post_Horario_ReturnsBadRequest_QuandoDiaSemanaInvalido`).
- [ ] Checks finais: `dotnet format && dotnet test`.
