import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type AlunoProvisorioConfirmadoProps = {
  nome: string;
};

/**
 * Molécula: confirmação inline de cadastro de Aluno provisório concluído
 * (issue #3) — sem indicação visual de "capacidade reduzida", o Aluno
 * provisório já pode ser agendado e cobrado normalmente (Regra de Negócio
 * do card).
 */
export function AlunoProvisorioConfirmado({ nome }: AlunoProvisorioConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Aluno provisório cadastrado!</Heading>
      <Paragraph>{nome} já pode ser agendado e cobrado normalmente.</Paragraph>
    </View>
  );
}
