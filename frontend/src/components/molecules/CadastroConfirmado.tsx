import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

/**
 * Molécula: confirmação inline de cadastro concluído. Não há para onde
 * navegar ainda (login/área logada é a issue #18, ortogonal a este card).
 */
export function CadastroConfirmado() {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Cadastro concluído!</Heading>
      <Paragraph>Seu cadastro como Professor foi realizado com sucesso.</Paragraph>
    </View>
  );
}
