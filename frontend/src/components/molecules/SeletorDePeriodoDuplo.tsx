import { View } from 'react-native';

import { SeletorDeData } from '@/components/molecules/SeletorDeData';

export type SeletorDePeriodoDuploProps = {
  inicio: string | undefined;
  fim: string | undefined;
  onSelecionarInicio: (dataISO: string) => void;
  onSelecionarFim: (dataISO: string) => void;
};

/**
 * Molécula: par de `SeletorDeData` (Início/Fim) lado a lado — extraída de
 * `aluno/historico-frequencia.tsx`/`aluno/valor-devido.tsx` (issue #116,
 * achado de dev-review: as duas telas duplicavam a mesma função local
 * `PeriodoSelecionado`) pra não manter duas cópias do mesmo par.
 */
export function SeletorDePeriodoDuplo({
  inicio,
  fim,
  onSelecionarInicio,
  onSelecionarFim,
}: SeletorDePeriodoDuploProps) {
  return (
    <View className="flex-row gap-three">
      <View className="flex-1">
        <SeletorDeData label="Início" valor={inicio} onSelecionar={onSelecionarInicio} />
      </View>
      <View className="flex-1">
        <SeletorDeData label="Fim" valor={fim} onSelecionar={onSelecionarFim} />
      </View>
    </View>
  );
}
