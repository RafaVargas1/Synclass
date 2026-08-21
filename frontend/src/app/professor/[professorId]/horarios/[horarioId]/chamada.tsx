import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FrequenciaAlunoToggle } from '@/components/organisms/FrequenciaAlunoToggle';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarAlocacoes } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios, type AlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { registrarFrequencia, type RegistroFrequenciaItem } from '@/lib/api/frequencias';
import { MaxContentWidth } from '@/theme/tokens';

const MensagemSucesso = 'Chamada salva com sucesso.';

/**
 * Tela de chamada do Professor (issue #14): lista os Alunos alocados no
 * horário (reaproveita `listarAlocacoes`/`listarAlunosProvisorios`, mesma
 * composição já usada por `alocacoes.tsx`/`HorarioAlocacaoCard` — não existe
 * endpoint dedicado de "alunos alocados numa data", ver
 * docs/specs/14-registro-frequencia/implementation.md) com um
 * `FrequenciaAlunoToggle` por Aluno e um botão que envia o lote de uma vez
 * via `registrarFrequencia`. `data` chega por query string (`?data=`, mesmo
 * padrão de parâmetro opcional descrito no implementation.md), não por
 * segmento de rota.
 */
export default function ChamadaScreen() {
  const { horarioId, data } = useLocalSearchParams<{
    professorId: string;
    horarioId: string;
    data?: string;
  }>();
  const dataEfetiva = data ?? dataDeHoje();
  const carregamento = useCarregamentoAlunosAlocados(horarioId);
  const salvamento = useSalvamentoChamada(horarioId, dataEfetiva, carregamento.alunos);

  if (carregamento.status === 'carregando') {
    return <TelaCarregando />;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Chamada" />
      <View className="w-full flex-1 self-center gap-four px-four py-four" style={{ maxWidth: MaxContentWidth }}>
        {carregamento.erro ? <ErrorMessage>{carregamento.erro}</ErrorMessage> : null}
        {salvamento.erro ? <ErrorMessage>{salvamento.erro}</ErrorMessage> : null}
        {salvamento.sucesso ? (
          <ErrorMessage className="text-primary dark:text-dark-primary">
            {MensagemSucesso}
          </ErrorMessage>
        ) : null}
        <FlatList
          data={carregamento.alunos}
          keyExtractor={(item) => item.matriculaId}
          renderItem={({ item }) => (
            <FrequenciaAlunoToggle
              aluno={item}
              presente={salvamento.presencaPorMatricula[item.matriculaId] ?? true}
              onChange={salvamento.handleChange}
            />
          )}
          contentContainerClassName="gap-two"
        />
        <Button
          label="Salvar chamada"
          disabled={salvamento.salvando || carregamento.alunos.length === 0}
          onPress={salvamento.handleSalvar}
        />
      </View>
    </SafeAreaView>
  );
}

/** Fallback quando a tela é acessada sem `?data=` — hoje, formato `yyyy-MM-dd` (contrato do backend). */
function dataDeHoje(): string {
  return new Date().toISOString().slice(0, 10);
}

function TelaCarregando() {
  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-background dark:bg-dark-background">
      <ActivityIndicator accessibilityLabel="Carregando" />
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando'; alunos: AlunoProvisorio[]; erro: undefined }
  | { status: 'carregada'; alunos: AlunoProvisorio[]; erro: string | undefined };

/**
 * Carrega os Alunos alocados ao montar, combinando `listarAlocacoes` (issue
 * #8) com `listarAlunosProvisorios` (issue #3) — mesma composição de
 * `alocacoes.tsx#carregarTudo`, aqui restrita a este único horário.
 */
function useCarregamentoAlunosAlocados(horarioId: string): EstadoCarregamento {
  const [estado, setEstado] = useState<EstadoCarregamento>({
    status: 'carregando',
    alunos: [],
    erro: undefined,
  });

  useEffect(() => {
    let cancelado = false;
    carregarAlunosAlocados(horarioId).then((resultado) => {
      if (!cancelado) setEstado({ status: 'carregada', ...resultado });
    });
    return () => {
      cancelado = true;
    };
  }, [horarioId]);

  return estado;
}

async function carregarAlunosAlocados(
  horarioId: string,
): Promise<{ alunos: AlunoProvisorio[]; erro: string | undefined }> {
  const [resultadoAlocacoes, resultadoAlunos] = await Promise.all([
    listarAlocacoes(horarioId),
    listarAlunosProvisorios(),
  ]);
  if (!resultadoAlocacoes.sucesso) {
    return { alunos: [], erro: resultadoAlocacoes.mensagem };
  }
  if (!resultadoAlunos.sucesso) {
    return { alunos: [], erro: resultadoAlunos.mensagem };
  }
  const matriculasAlocadas = new Set(
    resultadoAlocacoes.alocacoes.map((alocacao) => alocacao.matriculaId),
  );
  const alunos = resultadoAlunos.alunos.filter((aluno) =>
    matriculasAlocadas.has(aluno.matriculaId),
  );
  return { alunos, erro: undefined };
}

/**
 * Estado local de presente/ausente por Aluno e o envio do lote para
 * `registrarFrequencia` — extraído do componente para caber no orçamento de
 * linhas por função (ver docs/spec/code-style.md).
 */
function useSalvamentoChamada(horarioId: string, data: string, alunos: AlunoProvisorio[]) {
  const [presencaPorMatricula, setPresencaPorMatricula] = useState<Record<string, boolean>>({});
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [sucesso, setSucesso] = useState(false);

  const handleChange = (matriculaId: string, presente: boolean) => {
    setSucesso(false);
    setPresencaPorMatricula((atual) => ({ ...atual, [matriculaId]: presente }));
  };

  const handleSalvar = async () => {
    setSalvando(true);
    setErro(undefined);
    setSucesso(false);
    const registros = montarRegistros(alunos, presencaPorMatricula);
    const resultado = await registrarFrequencia(horarioId, data, registros);
    setSalvando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setSucesso(true);
  };

  return { presencaPorMatricula, salvando, erro, sucesso, handleChange, handleSalvar };
}

function montarRegistros(
  alunos: AlunoProvisorio[],
  presencaPorMatricula: Record<string, boolean>,
): RegistroFrequenciaItem[] {
  return alunos.map((aluno) => ({
    matriculaId: aluno.matriculaId,
    presente: presencaPorMatricula[aluno.matriculaId] ?? true,
  }));
}
