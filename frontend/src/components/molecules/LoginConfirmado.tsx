import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

/**
 * Molécula: confirmação inline de login concluído (issue #18). Mesma
 * decisão de CadastroConfirmado — não há área logada para navegar ainda,
 * então a confirmação fica na própria tela.
 */
export function LoginConfirmado({ nome }: { nome: string }) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Login realizado!</Heading>
      <Paragraph>Bem-vindo(a) de volta, {nome}.</Paragraph>
    </View>
  );
}
