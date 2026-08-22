import { Text, View } from 'react-native';

import { formatarData } from '@/lib/formatarData';

export type ProximoHorario = {
  professorNome: string;
  data: string;
  horaInicio: string;
};

/**
 * Organismo: card de resumo do "próximo horário" do Aluno no Painel (issue
 * #165). `proximoHorario` já chega resolvido pela tela (aggregate das
 * próximas aulas por todos os vínculos) — este componente só formata e
 * exibe. Mesma família visual de `ResumoValorReceber.tsx` (issue #166):
 * `View` puro, não-tocável, mesmas classes de borda/fundo. `horaInicio`
 * (achado de dev-review, PR #176) truncado com `.slice(0, 5)` — mesmo
 * padrão `HH:MM` de todo outro componente que já consome esse campo
 * (`AulaProximaCard.tsx`, `HorarioCard.tsx`, etc.), nunca `HH:MM:SS` cru.
 */
export function ResumoProximoHorario({ proximoHorario }: { proximoHorario: ProximoHorario | null }) {
  return (
    <View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">Próximo horário</Text>
      {proximoHorario ? (
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {`${proximoHorario.professorNome} — ${formatarData(proximoHorario.data)} às ${proximoHorario.horaInicio.slice(0, 5)}`}
        </Text>
      ) : (
        <Text className="text-base text-text-secondary dark:text-dark-text-secondary">
          Nenhum horário marcado no momento.
        </Text>
      )}
    </View>
  );
}
