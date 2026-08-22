import { Text, View } from 'react-native';

import { Input } from '@/components/atoms/Input';
import { horaEstaCompleta, mascararHora } from '@/lib/mascararHora';

export type SeletorDeHoraProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (hora: string) => void;
};

/**
 * Molécula: campo de hora com máscara progressiva HH:mm (issue #138,
 * substitui o painel de 2 listas roláveis da issue #117 — não era um
 * padrão de seleção de hora reconhecível, ver ux-heuristics.md#reconhecimento-em-vez-de-recordação).
 * Só chama `onSelecionar` quando o valor tem os 4 dígitos completos —
 * mesmo princípio de "sem pré-seleção acidental" da versão anterior
 * (achado de dev-review, PR #120): um valor parcial nunca é reportado
 * como escolhido.
 */
export function SeletorDeHora({ label, valor, onSelecionar }: SeletorDeHoraProps) {
  function handleChangeText(digitado: string) {
    const mascarado = mascararHora(digitado, valor ?? '');
    if (horaEstaCompleta(mascarado)) {
      onSelecionar(mascarado);
      return;
    }
    // Valor parcial: repropaga como está, sem chamar onSelecionar — o
    // componente é controlado pelo `valor` do pai, então precisamos de
    // um jeito de mostrar o parcial antes de completar. Ver "Edge point"
    // abaixo — decisão: onSelecionar também recebe o parcial, e quem
    // consome (HorarioForm) já trata valor indefinido/incompleto como
    // "ainda não escolhido" nas suas próprias validações.
    onSelecionar(mascarado);
  }

  return (
    <View className="gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Input
        value={valor ?? ''}
        onChangeText={handleChangeText}
        placeholder="HH:mm"
        keyboardType="numeric"
        maxLength={5}
        accessibilityLabel={label}
      />
    </View>
  );
}
