# Implementação: identificador de Aluno provisório gerado pelo sistema (#159)

## Entidades/classes afetadas

- `backend/src/Synclass.Domain/Matriculas/CadastroAlunoProvisorioService.cs` — `CadastrarAsync` para de receber `identificador` como parâmetro.
- `backend/src/Synclass.Api/Controllers/AlunosProvisoriosController.cs` — `CadastroAlunoProvisorioRequest` perde o campo `Identificador`; `Cadastrar` para de passar identificador pro Service.
- `frontend/src/lib/api/alunosProvisorios.ts` — `CadastroAlunoProvisorioInput` perde `identificador`.
- `frontend/src/components/organisms/CadastroAlunoProvisorioForm.tsx` — remove o campo "Identificador".
- `frontend/src/components/molecules/AlunoProvisorioConfirmado.tsx` — mostra o identificador devolvido.
- `frontend/src/app/professor/alunos/cadastro.tsx` — repassa o identificador do resultado pra confirmação.

**Não precisa de migration** — `IdentificadorProvisorio`/`IdentificadorAluno` continuam colunas existentes, só muda quem preenche `IdentificadorProvisorio` (antes: o texto digitado pelo Professor; depois: o mesmo valor gerado por `IdentificadorAlunoService`, que já é obrigatoriamente único no sistema inteiro — trivialmente único também por Professor).

## Backend

### `CadastroAlunoProvisorioService.cs` — estado atual

```csharp
public async Task<Matricula> CadastrarAsync(
    Guid professorId, string nome, string identificador, CancellationToken cancellationToken)
{
    var nomeValidado = ValidarNome(nome);
    var identificadorValidado = IdentificadorProvisorio.Validar(identificador);

    await GarantirProfessorExisteAsync(professorId, cancellationToken);
    await GarantirIdentificadorDisponivelAsync(professorId, identificadorValidado, cancellationToken);

    var identificadorAluno = await _identificadorAluno.GerarUnicoAsync(cancellationToken);
    var matricula = Matricula.CriarProvisoria(professorId, nomeValidado, identificadorValidado, identificadorAluno, _clock);
    await _matriculas.AdicionarAsync(matricula, cancellationToken);
    await _matriculas.SalvarAsync(cancellationToken);
    return matricula;
}
```

### Mudança

```csharp
public async Task<Matricula> CadastrarAsync(
    Guid professorId, string nome, CancellationToken cancellationToken)
{
    var nomeValidado = ValidarNome(nome);

    await GarantirProfessorExisteAsync(professorId, cancellationToken);

    var identificadorGerado = await _identificadorAluno.GerarUnicoAsync(cancellationToken);
    // O mesmo valor preenche IdentificadorProvisorio (mantém o índice único
    // por Professor satisfeito trivialmente — o valor já é único no
    // sistema inteiro) e IdentificadorAluno — não há mais dois
    // identificadores distintos pra um Aluno provisório, só um, gerado.
    var matricula = Matricula.CriarProvisoria(professorId, nomeValidado, identificadorGerado, identificadorGerado, _clock);
    await _matriculas.AdicionarAsync(matricula, cancellationToken);
    await _matriculas.SalvarAsync(cancellationToken);
    return matricula;
}
```

Remova `GarantirIdentificadorDisponivelAsync` (o método inteiro) — não há mais um valor digitado pra checar disponibilidade, `IdentificadorAlunoService.GerarUnicoAsync` já garante unicidade internamente (retry contra `IIdentificadorAlunoUnicidadeChecker`). O `using`/campo `IdentificadorProvisorioDuplicadoException` só era lançado por esse método removido — confirme com `grep -rn "IdentificadorProvisorioDuplicadoException"` se sobra alguma outra referência antes de decidir se a exception em si também deve ser removida (provavelmente sim, mas só se não for usada em mais nenhum lugar — não é o foco desta Task, pode deixar como código não utilizado se preferir não arriscar quebrar um teste que a referencia diretamente).

`IdentificadorProvisorio.Validar` (o validador de formato do texto digitado) também fica sem uso — mesma lógica: remova só se `grep -rn "IdentificadorProvisorio.Validar"` confirmar que não sobra nenhuma outra chamada.

### `AlunosProvisoriosController.cs` — estado atual (records no fim do arquivo)

```csharp
public sealed record CadastroAlunoProvisorioRequest(string Nome, string Identificador);
public sealed record CadastroAlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);
public sealed record AlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);
```

```csharp
[HttpPost]
public async Task<IActionResult> Cadastrar(
    [FromBody] CadastroAlunoProvisorioRequest request, CancellationToken cancellationToken)
{
    var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
    var professorId = User.GetUsuarioId();

    try
    {
        var matricula = await _cadastroAlunoProvisorio.CadastrarAsync(
            professorId, request.Nome, request.Identificador, cancellationToken);
        LogCadastroSucesso(trackId, matricula);
        return Ok(new CadastroAlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio!, matricula.IdentificadorProvisorio!));
    }
    ...
}
```

### Mudança

```csharp
public sealed record CadastroAlunoProvisorioRequest(string Nome);
public sealed record CadastroAlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);
public sealed record AlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);
```

`AlunoProvisorioResponse` (usado por `Listar`) não muda de forma — continua devolvendo `Identificador` (agora sempre o valor gerado, nunca mais digitado).

```csharp
[HttpPost]
public async Task<IActionResult> Cadastrar(
    [FromBody] CadastroAlunoProvisorioRequest request, CancellationToken cancellationToken)
{
    var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
    var professorId = User.GetUsuarioId();

    try
    {
        var matricula = await _cadastroAlunoProvisorio.CadastrarAsync(
            professorId, request.Nome, cancellationToken);
        LogCadastroSucesso(trackId, matricula);
        return Ok(new CadastroAlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio!, matricula.IdentificadorProvisorio!));
    }
    ...
}
```

`catch (MatriculaRejeitadaException ex)` no `Cadastrar` continua existindo (nome inválido ainda pode lançar `NomeProvisorioInvalidoException`, que é uma `MatriculaRejeitadaException`) — não remova esse catch.

## Frontend

### `frontend/src/lib/api/alunosProvisorios.ts`

```ts
// antes
export type CadastroAlunoProvisorioInput = { nome: string; identificador: string };

function postCadastro(input: CadastroAlunoProvisorioInput): Promise<Response> {
  return fetchComTimeout('/professores/alunos-provisorios', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: input.nome, identificador: input.identificador }),
  });
}
```

```ts
// depois
export type CadastroAlunoProvisorioInput = { nome: string };

function postCadastro(input: CadastroAlunoProvisorioInput): Promise<Response> {
  return fetchComTimeout('/professores/alunos-provisorios', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: input.nome }),
  });
}
```

`CadastroAlunoProvisorioResultado` já tem `identificador` no caso de sucesso — não muda (a Api continua devolvendo o campo, agora sempre gerado).

### `CadastroAlunoProvisorioForm.tsx`

Remove o `FormField` de "Identificador" e a prop `identificador`/`onChangeIdentificador` inteiras:

```tsx
// remove estas duas props da assinatura e o FormField correspondente:
//   identificador: string;
//   onChangeIdentificador: (identificador: string) => void;
//
// remove este bloco do JSX:
//   <FormField
//     label="Identificador"
//     value={identificador}
//     onChangeText={onChangeIdentificador}
//     placeholder="Identificador (ex: número de matrícula)"
//   />
```

### `frontend/src/app/professor/alunos/cadastro.tsx`

```tsx
// antes
const [nome, setNome] = useState('');
const [identificador, setIdentificador] = useState('');
...
const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);

async function handleSubmit() {
  ...
  const resultado = await cadastrarAlunoProvisorio({ nome, identificador });
  ...
  setNomeConfirmado(resultado.nome);
}
...
{nomeConfirmado ? <AlunoProvisorioConfirmado nome={nomeConfirmado} /> : (
  <CadastroAlunoProvisorioForm
    nome={nome}
    identificador={identificador}
    ...
    onChangeIdentificador={setIdentificador}
    ...
  />
)}
```

```tsx
// depois
const [nome, setNome] = useState('');
...
const [confirmado, setConfirmado] = useState<
  { nome: string; identificador: string } | undefined
>(undefined);

async function handleSubmit() {
  ...
  const resultado = await cadastrarAlunoProvisorio({ nome });
  ...
  setConfirmado({ nome: resultado.nome, identificador: resultado.identificador });
}
...
{confirmado ? (
  <AlunoProvisorioConfirmado nome={confirmado.nome} identificador={confirmado.identificador} />
) : (
  <CadastroAlunoProvisorioForm nome={nome} ... />
)}
```

### `AlunoProvisorioConfirmado.tsx` — estado atual

```tsx
export type AlunoProvisorioConfirmadoProps = { nome: string };

export function AlunoProvisorioConfirmado({ nome }: AlunoProvisorioConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Aluno provisório cadastrado!</Heading>
      <Paragraph>{nome} já pode ser agendado e cobrado normalmente.</Paragraph>
    </View>
  );
}
```

### Mudança

```tsx
export type AlunoProvisorioConfirmadoProps = { nome: string; identificador: string };

export function AlunoProvisorioConfirmado({ nome, identificador }: AlunoProvisorioConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Aluno provisório cadastrado!</Heading>
      <Paragraph>{nome} já pode ser agendado e cobrado normalmente.</Paragraph>
      <Paragraph>
        Identificador: <Text className="font-semibold text-text dark:text-dark-text">{identificador}</Text>
      </Paragraph>
    </View>
  );
}
```

(Import `Text` de `react-native` no topo do arquivo.)

## Testes a ajustar/criar

- Backend: `CadastroAlunoProvisorioServiceTests` — remova qualquer teste sobre `IdentificadorProvisorioInvalidoException`/`IdentificadorProvisorioDuplicadoException` vindo do valor digitado (não existem mais); ajuste a assinatura de `CadastrarAsync` em todos os call sites de teste. Adicione um teste confirmando que `IdentificadorProvisorio` e `IdentificadorAluno` da `Matricula` resultante são iguais ao valor gerado.
- Backend: `AlunosProvisoriosControllerTests`/testes de fumaça — `CadastroAlunoProvisorioRequest` sem `Identificador` no corpo da requisição.
- Frontend: `CadastroAlunoProvisorioForm.test.tsx` — remova asserts sobre o campo "Identificador"; ajuste props do render.
- Frontend: `AlunoProvisorioConfirmado.test.tsx` (criar se não existir) — assert que o identificador devolvido aparece na tela.
- Frontend: `frontend/src/app/professor/alunos/cadastro.test.tsx` — ajuste o mock de `cadastrarAlunoProvisorio` pra não enviar `identificador` no input e confirme que o identificador devolvido é passado pra `AlunoProvisorioConfirmado`.

## Fora de escopo

- Não mexer em `IGeradorDeIdentificadorAluno`/`GeradorDeIdentificadorAluno.cs` (formato `ALU-XXXX`) — reaproveitar como está.
- Não mexer no fluxo de convite/promoção de matrícula (`Matricula.Promover`) nem em `ConviteService`.
- Não remover a coluna/índice `IdentificadorProvisorio` do banco — só muda quem a preenche.
