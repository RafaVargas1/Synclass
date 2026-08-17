import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

/**
 * Molécula: estado de convite expirado (issue #2, critério de aceite 3) —
 * distinto do erro genérico de formulário porque reenviar os mesmos dados
 * nunca resolve (o token está morto); a única ação possível é o Aluno pedir
 * um novo link ao Professor.
 */
export function ConviteExpirado() {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Convite expirado</Heading>
      <Paragraph>Peça ao Professor para gerar um novo link de convite.</Paragraph>
    </View>
  );
}
