# Implementação: mensagem clara na tela de Alocação de Alunos (#139)

## Estado atual (lido de `frontend/src/app/professor/[professorId]/alocacoes.tsx`)

```tsx
const MensagemModeloVagoTexto =
  'O modelo de agendamento Vago não usa atribuição fixa de Aluno a horário.';

export default function AlocacoesProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const carregamento = useCarregamentoConfiguracao(professorId);

  if (carregamento.status === 'carregando') return <TelaCarregando />;
  if (carregamento.status === 'falha') return <TelaErroConfiguracao .../>;

  return (
    <SafeAreaView ...>
      <TopbarAutenticada titulo="Alocação de Alunos" />
      <View ...>
        {permiteAlocacao(carregamento) ? (
          <AlocacoesConteudo professorId={professorId} />
        ) : (
          <ErrorMessage>{MensagemModeloVagoTexto}</ErrorMessage>
        )}
      </View>
    </SafeAreaView>
  );
}

function permiteAlocacao(carregamento) {
  return carregamento.definida && carregamento.modeloAgendamento !== ModeloAgendamento.Vago;
}
```

Dois problemas, ambos confirmados por leitura de código (não suposição):
1. `MensagemModeloVagoTexto` cita "Vago" — nome do enum do backend
   (`ModeloAgendamento.Vago = 0`), nunca mostrado ao usuário em nenhuma
   outra tela. O rótulo público equivalente, usado em `HorarioForm`/
   `HorarioCard` (`OpcoesTipoMarcacao`, `RotulosTipoMarcacao`), é
   **"Livre"**.
2. A mesma mensagem aparece tanto quando `!carregamento.definida` (modelo
   nunca configurado) quanto quando o modelo é Livre — dois cenários
   diferentes que o usuário não consegue distinguir. E nenhum dos dois
   cenários verifica se existem horários cadastrados — um Professor sem
   nenhum horário vê uma mensagem sobre "modelo", que não é a causa real.

## Desenho novo

`AlocacoesConteudo` já busca horários/alunos/alocações
(`useGerenciamentoAlocacoes`, mesma função) — a mudança é buscar os
horários **sempre**, independente do modelo, e decidir qual estado mostrar
combinando `definida` (config existe?) + `modeloAgendamento` + `horarios.length`.

### Novo componente de estado, em vez do `ErrorMessage` único

```tsx
type EstadoAlocacao =
  | { tipo: 'sem-horarios' }
  | { tipo: 'modelo-nao-configurado' }
  | { tipo: 'modelo-livre'; temAlunosInscritos: boolean }
  | { tipo: 'permite-alocacao' };

function resolverEstadoAlocacao(
  carregamento: Extract<EstadoCarregamento, { status: 'carregada' }>,
  horarios: Horario[],
  totalAlocacoes: number,
): EstadoAlocacao {
  if (horarios.length === 0) {
    return { tipo: 'sem-horarios' };
  }
  if (!carregamento.definida) {
    return { tipo: 'modelo-nao-configurado' };
  }
  if (carregamento.modeloAgendamento === ModeloAgendamento.Vago) {
    return { tipo: 'modelo-livre', temAlunosInscritos: totalAlocacoes > 0 };
  }
  return { tipo: 'permite-alocacao' };
}
```

`horarios` precisa estar disponível ANTES da decisão — isso exige mover o
fetch de horários pra fora do `permiteAlocacao ? <AlocacoesConteudo> : ...`
atual (hoje só busca depois de já estar liberado). Estrutura sugerida: um
hook novo `useEstadoAlocacao(professorId)` que:
1. Busca `obterConfiguracao(professorId)` E `listarHorarios(professorId)`
   em paralelo (`Promise.all`, mesmo padrão já usado em
   `carregarTudo` dentro de `useGerenciamentoAlocacoes`).
2. Se qualquer um falhar, mesmo estado de erro/retry já existente
   (`TelaErroConfiguracao`) — não precisa mudar esse caminho.
3. Quando ambos resolvem, calcula `resolverEstadoAlocacao` acima. Se
   `tipo === 'modelo-livre'`, também precisa saber se há alguma alocação
   (`listarAlocacoes` por horário, mesma função já usada em
   `carregarAlocacoes` de `useGerenciamentoAlocacoes`) pra decidir
   `temAlunosInscritos`.

Isso duplica uma busca de horários que `AlocacoesConteudo`/
`useGerenciamentoAlocacoes` já faz quando o estado é `permite-alocacao` —
aceitável (dado pequeno, sem paginação hoje) desde que não vire uma
segunda fonte de verdade divergente: quando `resolverEstadoAlocacao`
retornar `'permite-alocacao'`, renderize `<AlocacoesConteudo
professorId={professorId} />` normalmente (ela refaz a busca sozinha, como
já faz hoje) — não tente reaproveitar os dados já buscados pra evitar
complicar o hook com cache manual, fora do escopo desta Task.

### Mensagens por estado (rótulo exato — não parafrasear)

```tsx
function MensagemDoEstado({ estado }: { estado: EstadoAlocacao }) {
  switch (estado.tipo) {
    case 'sem-horarios':
      return (
        <View className="items-center gap-three">
          <ErrorMessage>Nenhum horário cadastrado ainda.</ErrorMessage>
          <Link href={`/professor/${professorId}/horarios`} asChild>
            <Button label="Cadastrar horários" variante="secundario" />
          </Link>
        </View>
      );
    case 'modelo-nao-configurado':
      return <ErrorMessage>Configure o modelo de agendamento antes de alocar Alunos.</ErrorMessage>;
    case 'modelo-livre':
      return (
        <ErrorMessage>
          {estado.temAlunosInscritos
            ? 'No modelo Livre, os Alunos se inscrevem sozinhos — não há atribuição manual pelo Professor.'
            : 'No modelo Livre, os Alunos se inscrevem sozinhos. Ainda ninguém se inscreveu em nenhum horário.'}
        </ErrorMessage>
      );
  }
}
```

(`professorId` vem do escopo do componente pai — ajuste a assinatura como
fizer sentido, ex: passar como prop.) Não crie um componente `Button`
"secundario" novo — `variante="secundario"` já existe em
`frontend/src/components/atoms/Button.tsx`, use como está.

## Testes (`alocacoes.test.tsx`, já existe — estender)

- Mock `listarHorarios` retornando `[]` → mostra "Nenhum horário
  cadastrado ainda." + link "Cadastrar horários", NÃO mostra nada sobre
  modelo.
- Mock `obterConfiguracao` com `definida: false`, horários não-vazios →
  "Configure o modelo de agendamento antes de alocar Alunos."
- Mock modelo `Vago` (Livre), horários não-vazios, `listarAlocacoes`
  retornando vazio pra todos → mensagem "ninguém se inscreveu ainda".
- Mock modelo `Vago` (Livre), horários não-vazios, alguma alocação
  existente → mensagem sem o "ninguém se inscreveu".
- Mock modelo `Fixo`/`Hibrido`, horários não-vazios → comportamento atual
  preservado (`AlocacoesConteudo` renderiza, sem `ErrorMessage`).
- **Nenhum teste deve conter a string "Vago"** — grep no arquivo de teste
  final pra confirmar.

## Fora de escopo

- Não mexer em `useGerenciamentoAlocacoes`/`AlocacoesConteudo` além do
  necessário pra este novo fluxo de decisão de estado.
- Não mexer em `horarios.tsx` (tela de gerenciar horários) — só o link de
  destino do CTA "Cadastrar horários" precisa apontar pra rota certa
  (`/professor/{professorId}/horarios`, já existe).
- Fecha #126 (mesmo achado, causa raiz agora confirmada e detalhada aqui).
