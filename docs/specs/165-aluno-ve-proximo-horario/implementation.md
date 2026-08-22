# Implementação: Aluno vê próximo horário marcado direto no Painel (#165)

## Decisão de arquitetura (resolvida — ver task.md para a investigação completa)

**Escolhida: alternativa A** — criar `GET /alunos/professores`, endpoint
mínimo que devolve os vínculos (professorId + nome) do Aluno autenticado.
A RN do card autoriza endpoint novo exatamente quando não há hoje forma de
listar horários futuros por todos os Professores de uma vez — a
investigação confirmou que não há, e que o fallback literal da RN (usar
vínculos de `usePerfilLogado`) não é executável porque esse hook nunca
carregou vínculos. `GET /alunos/professores` é a fonte mínima que falta
pra viabilizar o "agregar no frontend" que a RN pedia — sem duplicar
lógica de horário/aula no backend (alternativa B, rejeitada: proxy via
frequência é semanticamente errado e caro; C, rejeitada: contradiz a
história de usuário).

## Backend: `GET /alunos/professores` (endpoint novo)

### Controller novo: `backend/src/Synclass.Api/Controllers/VinculosAlunoController.cs`

Segue o padrão de `AlunosProvisoriosController.cs` (autorização por Role,
`professorId`/`alunoId` sempre do token, nunca de rota/query) e de
`HistoricoFrequenciaController.cs` (rota `alunos/...`, retorna nome do
Professor resolvido via `IUsuarioRepository`):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Vínculos (Professores) do Aluno autenticado (issue #165) — alimenta o
/// resumo de "próximo horário" do Painel, que precisa agregar
/// `GET /professores/{professorId}/horarios/proximas-aulas` por todos os
/// Professores do Aluno. Não existe hoje outra forma de obter essa lista
/// (ver docs/specs/165-aluno-ve-proximo-horario/task.md).
/// </summary>
[Authorize(Roles = "Aluno")]
[ApiController]
[Route("alunos/professores")]
public sealed class VinculosAlunoController : ControllerBase
{
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;

    public VinculosAlunoController(IMatriculaRepository matriculas, IUsuarioRepository usuarios)
    {
        _matriculas = matriculas;
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var alunoId = User.GetUsuarioId();
        var matriculas = await _matriculas.ListarPorAlunoAsync(alunoId, cancellationToken);

        var professorIds = matriculas.Select(m => m.ProfessorId).Distinct().ToList();
        var vinculos = new List<VinculoProfessorResponse>();
        foreach (var professorId in professorIds)
        {
            var professor = await _usuarios.BuscarPorIdAsync(professorId, cancellationToken);
            if (professor is not null)
            {
                vinculos.Add(new VinculoProfessorResponse(professorId, professor.Nome));
            }
        }
        return Ok(vinculos);
    }
}

public sealed record VinculoProfessorResponse(Guid ProfessorId, string Nome);
```

Sem matrícula, devolve lista vazia (`Ok([])`) — não é erro, é o Aluno sem
nenhum vínculo ainda (cenário 2 do card).

**Teste de fumaça**: `backend/tests/Synclass.Api.Tests/VinculosAlunoEndpointTests.cs`
(criar) — mesmo padrão de `AlunosProvisoriosController`/`ClienteAutenticadoComoAlunoPersistidoAsync`
já usado em outros arquivos desta pasta (grep por
`AutenticacaoTestHelper` pra ver a assinatura exata antes de escrever).
Cenários: Aluno com matrícula em dois Professores → lista os dois, sem
duplicar quando há mais de uma matrícula com o mesmo Professor (`Distinct`);
Aluno sem nenhuma matrícula → lista vazia, 200 (não 404).

## Frontend

### `frontend/src/lib/api/vinculosAluno.ts` (novo)

Mesmo padrão de `frontend/src/lib/api/historicoFrequencia.ts` (nunca
lança, sempre devolve resultado tipado):

```ts
import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type VinculoProfessor = { professorId: string; nome: string };

export type ListarVinculosAlunoResultado =
  | { sucesso: true; vinculos: VinculoProfessor[] }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

export async function listarVinculosAluno(): Promise<ListarVinculosAlunoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout('/alunos/professores');
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  const corpo = await response.json().catch(() => null);
  return { sucesso: true, vinculos: (corpo as VinculoProfessor[] | null) ?? [] };
}
```

### `frontend/src/components/organisms/ResumoProximoHorario.tsx` (novo)

Mesma família visual/edge-cases de `ResumoValorReceber.tsx` (issue #166,
já implementado nesta mesma leva — leia esse arquivo antes de escrever
este, é o padrão de card de resumo do Painel a seguir: `View` puro,
não-tocável, mesmas classes de borda/fundo). Props: `proximoHorario:
{ professorNome: string; data: string; horaInicio: string } | null`
(já resolvido pelo Painel, este componente só formata/exibe):

```tsx
export function ResumoProximoHorario({ proximoHorario }: { proximoHorario: { professorNome: string; data: string; horaInicio: string } | null }) {
  return (
    <View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">Próximo horário</Text>
      {proximoHorario ? (
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {`${proximoHorario.professorNome} — ${formatarData(proximoHorario.data)} às ${proximoHorario.horaInicio}`}
        </Text>
      ) : (
        <Text className="text-base text-text-secondary dark:text-dark-text-secondary">
          Nenhum horário marcado no momento.
        </Text>
      )}
    </View>
  );
}
```

Use `formatarData` já existente em `frontend/src/lib/formatarData.ts` (não
invente formatação de data nova — grep pelo uso em outra tela antes de
escrever a chamada exata).

### `frontend/src/app/painel/index.tsx`

Insere entre `Saudacao` e o grid de `CardDeAcao` (mesmo ponto de inserção
de `ResumoValorReceber` na issue #166 — se essa Task já mergeou quando
esta rodar, insira como irmã dela, resumo de Aluno depois do de
Professor não se aplica pois são condicionais por papel; leia o estado
atual do arquivo antes de editar). Só renderiza quando `papelAtivo ===
'Aluno'`:

- Ao montar (papel Aluno ativo): chama `listarVinculosAluno()`. Para cada
  `professorId` retornado, chama `listarProximasAulas(professorId)`
  (`frontend/src/lib/api/cancelamentos.ts`, já existe). Reduz para o
  `AulaProxima` com a `data`+`horaInicio` mais próxima no tempo entre
  todas (comparação de string ISO de data é suficiente se o formato for
  `YYYY-MM-DD` — confirme lendo `AulaProxima.data` antes de assumir).
  Junta com o `nome` do vínculo correspondente pra montar o
  `proximoHorario` de `ResumoProximoHorario`.
- Nenhum vínculo, ou nenhum `AulaProxima` em nenhum vínculo: `proximoHorario
  = null` → estado "Nenhum horário marcado" (cenário 2).
- Erro de qualquer uma das chamadas: não bloqueia o resto do Painel — loga
  silenciosamente ou mostra o card em estado "Nenhum horário marcado"
  (mesmo tratamento não-bloqueante de `ResumoValorReceber`/#166 — não
  invente um terceiro padrão de erro pro Painel).

## Testes

- `VinculosAlunoEndpointTests.cs`: ver acima.
- `frontend/src/lib/api/vinculosAluno.test.ts` (novo): sucesso, lista
  vazia, erro de rede — mesmo padrão de `historicoFrequencia.test.ts`.
- `frontend/src/components/organisms/ResumoProximoHorario.test.tsx`
  (novo): com `proximoHorario` preenchido mostra os dados formatados; com
  `null` mostra "Nenhum horário marcado no momento."
- `frontend/src/app/painel/index.test.tsx`: estende a suíte existente —
  mocka `listarVinculosAluno`/`listarProximasAulas`; cenário com um
  Professor mostra o horário dele; cenário com dois Professores mostra o
  mais próximo no tempo (não o do primeiro Professor da lista); cenário
  sem vínculo/sem aula futura mostra o estado vazio; papel Professor
  ativo não chama nenhuma das duas funções novas.

## Edge points

- Aluno com vínculo mas nenhum horário alocado ainda (matrícula existe,
  `proximas-aulas` desse Professor vazio): mesmo estado "Nenhum horário
  marcado" do cenário sem vínculo nenhum — a distinção não importa pro
  usuário.
- `IMatriculaRepository.ListarPorAlunoAsync` pode devolver mais de uma
  `Matricula` pro mesmo `ProfessorId` (não deveria pela regra de negócio
  atual, mas o `Distinct()` no controller já protege).

## Fora de escopo

- Não criar uma tela "Meus Professores" dedicada pro Aluno — só o resumo
  do Painel está em escopo aqui (a ausência dessa tela foi só um achado
  da investigação, não um requisito desta issue).
- Não mudar `HistoricoFrequenciaController`/`ValorDevidoAlunoController`
  pra incluir `professorId` (poderia evitar o endpoint novo em outra
  decisão, mas essa Task já resolveu com a alternativa A).
