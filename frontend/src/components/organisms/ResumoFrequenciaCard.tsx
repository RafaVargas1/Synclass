import { Text, View } from 'react-native';

export type ResumoFrequenciaCardProps = { presentes: number; ausentes: number };

const MensagemSemDados =
  'Você ainda não tem frequência registrada nos últimos 30 dias. Suas aulas aparecem aqui assim que alguma presença ou falta for marcada.';

/**
 * Forma singular/plural correta do rótulo de uma contagem no resumo — copy
 * fixado aqui (não é pra DeepSeek escolher). "1 presença", "2 presenças";
 * "1 falta", "2 faltas".
 */
function rotularPlural(quantidade: number, singular: string, plural: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : plural}`;
}

/**
 * Organismo: resumo de frequência recente do Aluno no Painel (issue #167) —
 * contagem de aulas `Presente`/`Ausente` nos últimos 30 dias, calculada a
 * partir do mesmo `GET /alunos/historico-frequencia` da tela de histórico
 * (issue #16), sem duplicar a lógica de cálculo (quem conta os status é o
 * `calcularResumo` no Painel, ver lá). Somente os dois status entram no par
 * "compareceu/faltou" — `NaoRegistrada`/`Cancelada` não são nem um nem outro
 * e mantêm o card no estado vazio. Mesmo esqueleto visual de
 * `HistoricoFrequenciaCard`/`ValueDevidoCard` (borda
 * `border-background-selected`, fundo `bg-background-element`).
 */
export function ResumoFrequenciaCard({ presentes, ausentes }: ResumoFrequenciaCardProps) {
  const semDados = presentes === 0 && ausentes === 0;

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      {semDados ? (
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {MensagemSemDados}
        </Text>
      ) : (
        <>
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">Nos últimos 30 dias</Text>
          <Text className="text-sm font-semibold text-text dark:text-dark-text">
            {rotularPlural(presentes, 'presença', 'presenças')} ·{' '}
            {rotularPlural(ausentes, 'falta', 'faltas')}
          </Text>
        </>
      )}
    </View>
  );
}
