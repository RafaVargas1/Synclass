# Implementação: tela "Meus Alunos" (#160)

## Entidades/componentes afetados (todos novos, exceto o menu)

- **Novo**: `frontend/src/app/professor/[professorId]/alunos.tsx` — tela.
- **Novo**: `frontend/src/components/organisms/AlunoVinculadoCard.tsx` —
  card de um Aluno na lista.
- **Novo**: `frontend/src/lib/agruparAlocacoesPorAluno.ts` — função pura
  que cruza alunos + horários + alocações.
- **Editado**: `frontend/src/lib/secoesPorPapel.ts` — nova seção no menu
  do Professor.

**Sem endpoint de backend novo** — compõe três chamadas já existentes:
`listarAlunosProvisorios()` (`@/lib/api/alunosProvisorios`),
`listarHorarios(professorId)` (`@/lib/api/horarios`), `listarAlocacoes(horarioId)`
(`@/lib/api/alocacoes`, uma chamada por horário — mesmo padrão de
`alocacoes.tsx#carregarAlocacoes`, reaproveite a MESMA função em vez de
duplicá-la: exporte `carregarAlocacoes` de `alocacoes.tsx` não é prático
(arquivo de rota) — copie a função pequena (5 linhas) pra este novo
arquivo `agruparAlocacoesPorAluno.ts`, não importe de dentro de uma rota).

## `frontend/src/lib/agruparAlocacoesPorAluno.ts` (função pura, testável isolada)

```ts
import type { Alocacao } from './api/alocacoes';
import type { AlunoProvisorio } from './api/alunosProvisorios';
import type { Horario } from './api/horarios';

export type AlunoComHorarios = {
  matriculaId: string;
  nome: string;
  identificador: string;
  horarios: Horario[];
};

/**
 * Cruza a lista de Alunos do Professor com os horários em que cada um está
 * alocado (issue #160) — um Aluno pode estar em zero, um, ou mais
 * horários; `alocacoesPorHorario` já vem carregado (uma chamada de
 * `listarAlocacoes` por horário, feita por quem chama esta função).
 */
export function agruparAlocacoesPorAluno(
  alunos: AlunoProvisorio[],
  horarios: Horario[],
  alocacoesPorHorario: Record<string, Alocacao[]>,
): AlunoComHorarios[] {
  return alunos.map((aluno) => ({
    matriculaId: aluno.matriculaId,
    nome: aluno.nome,
    identificador: aluno.identificador,
    horarios: horarios.filter((horario) =>
      (alocacoesPorHorario[horario.id] ?? []).some((alocacao) => alocacao.matriculaId === aluno.matriculaId),
    ),
  }));
}
```

Teste (`agruparAlocacoesPorAluno.test.ts`):
- Aluno com uma alocação → `horarios` tem 1 item, o horário certo.
- Aluno com alocações em 2 horários diferentes → `horarios` tem os 2.
- Aluno sem nenhuma alocação → `horarios` é `[]` (não `undefined`).
- Lista de alunos vazia → retorna `[]`.
- `alocacoesPorHorario` sem entrada pra um horário (`{}` no lugar de
  `{ [id]: [] }`) não lança — trate como lista vazia (`?? []`, já no
  código acima).

## `frontend/src/components/organisms/AlunoVinculadoCard.tsx`

```tsx
import { Text, View } from 'react-native';

import type { AlunoComHorarios } from '@/lib/agruparAlocacoesPorAluno';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type AlunoVinculadoCardProps = { aluno: AlunoComHorarios };

/**
 * Organismo: card de um Aluno na tela "Meus Alunos" (issue #160) — nome,
 * identificador, e os horários em que está alocado (ou "Sem horário" se
 * nenhum). Mesmo estilo visual de card já usado em `HorarioCard`/
 * `ValorDevidoCard` (`rounded-medium border ... bg-background-element`),
 * não inventar um novo.
 */
export function AlunoVinculadoCard({ aluno }: AlunoVinculadoCardProps) {
  return (
    <View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{aluno.nome}</Text>
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
        {aluno.identificador}
      </Text>
      <Text testID="horarios-do-aluno" className="text-sm text-text-secondary dark:text-dark-text-secondary">
        {aluno.horarios.length === 0
          ? 'Sem horário'
          : aluno.horarios
              .map((horario) => `${NomesDiaSemana[horario.diaSemana]} · ${horario.horaInicio.slice(0, 5)}`)
              .join(', ')}
      </Text>
    </View>
  );
}
```

## `frontend/src/app/professor/[professorId]/alunos.tsx`

Mesmo esqueleto de tela de `frontend/src/app/professor/[professorId]/alocacoes.tsx`
(carregamento → erro com retry → conteúdo), sem o gate de estado
específico de alocação (aqui é só "tem Aluno ou não"):

```tsx
import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { AlunoVinculadoCard } from '@/components/organisms/AlunoVinculadoCard';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarAlocacoes, type Alocacao } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios, type AlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { listarHorarios, type Horario } from '@/lib/api/horarios';
import { agruparAlocacoesPorAluno } from '@/lib/agruparAlocacoesPorAluno';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela "Meus Alunos" (issue #160): visão geral de nome, identificador e
 * horário(s) de cada Aluno vinculado ao Professor — sem endpoint novo,
 * cruza `listarAlunosProvisorios`/`listarHorarios`/`listarAlocacoes` (uma
 * chamada por horário, mesmo padrão de `alocacoes.tsx`).
 */
export default function MeusAlunosScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useCarregamentoAlunos(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Meus Alunos" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {estado.status === 'carregando' ? (
          <ActivityIndicator accessibilityLabel="Carregando" />
        ) : estado.status === 'falha' ? (
          <View className="flex-1 items-center justify-center gap-four">
            <ErrorMessage>{estado.mensagem}</ErrorMessage>
            <Button label="Tentar novamente" onPress={estado.tentarNovamente} />
          </View>
        ) : estado.alunos.length === 0 ? (
          <ErrorMessage>Nenhum Aluno cadastrado ainda.</ErrorMessage>
        ) : (
          <FlatList
            data={estado.alunos}
            keyExtractor={(item) => item.matriculaId}
            renderItem={({ item }) => <AlunoVinculadoCard aluno={item} />}
            contentContainerClassName="gap-two"
            style={{ minHeight: 420, flexGrow: 1 }}
          />
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregado'; alunos: ReturnType<typeof agruparAlocacoesPorAluno> };

function useCarregamentoAlunos(professorId: string): EstadoCarregamento {
  const [resultado, setResultado] = useState<
    { sucesso: true; alunos: ReturnType<typeof agruparAlocacoesPorAluno> } | { sucesso: false; mensagem: string } | undefined
  >(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };

  useEffect(() => {
    let cancelado = false;
    carregarAlunosComHorarios(professorId).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, tentativa]);

  if (resultado === undefined) return { status: 'carregando' };
  if (!resultado.sucesso) return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  return { status: 'carregado', alunos: resultado.alunos };
}

async function carregarAlunosComHorarios(professorId: string) {
  const [resultadoAlunos, resultadoHorarios] = await Promise.all([
    listarAlunosProvisorios(),
    listarHorarios(professorId),
  ]);
  if (!resultadoAlunos.sucesso) {
    return { sucesso: false as const, mensagem: resultadoAlunos.mensagem };
  }
  if (!resultadoHorarios.sucesso) {
    return { sucesso: false as const, mensagem: resultadoHorarios.mensagem };
  }
  const alocacoesPorHorario = await carregarAlocacoes(resultadoHorarios.horarios);
  return {
    sucesso: true as const,
    alunos: agruparAlocacoesPorAluno(resultadoAlunos.alunos, resultadoHorarios.horarios, alocacoesPorHorario),
  };
}

async function carregarAlocacoes(horarios: Horario[]): Promise<Record<string, Alocacao[]>> {
  const resultados = await Promise.all(horarios.map((horario) => listarAlocacoes(horario.id)));
  const entradas = horarios.map((horario, indice) => {
    const resultado = resultados[indice];
    return [horario.id, resultado.sucesso ? resultado.alocacoes : []] as const;
  });
  return Object.fromEntries(entradas);
}
```

Se o arquivo passar de 500 linhas ou alguma função passar de 20 (checar
`docs/spec/code-style.md`), extraia `useCarregamentoAlunos`/
`carregarAlunosComHorarios`/`carregarAlocacoes` pra um hook próprio
(`useAlunosComHorarios.ts`), mesmo padrão de `useGerenciamentoAlocacoes`
em `alocacoes.tsx` — decisão mecânica, não pare pra perguntar.

## `frontend/src/lib/secoesPorPapel.ts`

```ts
// dentro de secoesProfessor, junto das demais que dependem de usuarioId:
{ label: 'Meus Alunos', href: `/professor/${usuarioId}/alunos` as Href },
```

Adicione logo antes ou depois de `'Gerenciar horários'` na lista (ordem
exata não é crítica, mas mantenha junto das outras ações que dependem de
`usuarioId`, não junto de `Cadastrar Aluno`, que não depende).

## Testes

- `agruparAlocacoesPorAluno.test.ts`: casos listados acima.
- `AlunoVinculadoCard.test.tsx`: mostra nome/identificador; mostra "Sem
  horário" quando `horarios: []`; mostra o(s) horário(s) formatados
  quando não vazio.
- `alunos.test.tsx` (tela): loading state; erro com retry (mock de
  qualquer uma das 3 chamadas falhando); lista vazia → "Nenhum Aluno
  cadastrado ainda."; lista com alunos, alguns com horário e outros sem,
  renderiza os cards certos.
- `secoesPorPapel.test.ts`: `secoesProfessor(usuarioId)` inclui a seção
  "Meus Alunos" com o href certo quando `usuarioId` está definido.

## Fora de escopo

- Não mexer em `HorarioAlocacaoCard.tsx`/`alocacoes.tsx` — tela
  read-only, sem alocar/desalocar por aqui.
- Não paginar a lista (mesmo padrão dos demais `FlatList` do app, sem
  paginação ainda).
- Nome/identificador de Aluno promovido (matrícula plena vinda de
  convite) pode vir vazio — limitação conhecida e pré-existente de
  `AlunosProvisoriosController.ParaResponse` (`string.Empty` quando não
  há `NomeProvisorio`), não corrigir aqui.
