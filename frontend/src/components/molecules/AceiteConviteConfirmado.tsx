import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type AceiteConviteConfirmadoProps = {
  nome: string;
};

/**
 * Molécula: confirmação inline de aceite de convite concluído (issue #2) —
 * o Aluno já está com papel Aluno e vinculado ao Professor que o convidou.
 */
export function AceiteConviteConfirmado({ nome }: AceiteConviteConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Cadastro concluído!</Heading>
      <Paragraph>
        {nome}, seu cadastro foi concluído e você já está vinculado ao Professor.
      </Paragraph>
    </View>
  );
}
