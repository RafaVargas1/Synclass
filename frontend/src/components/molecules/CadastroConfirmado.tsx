import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type CadastroConfirmadoProps = {
  /** Papel cadastrado (issue #61 generaliza este componente para Professor e Aluno). */
  papel: 'Professor' | 'Aluno';
};

/**
 * Molécula: confirmação inline de cadastro concluído. Não há para onde
 * navegar ainda (login/área logada é a issue #18, ortogonal a este card).
 */
export function CadastroConfirmado({ papel }: CadastroConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Cadastro concluído!</Heading>
      <Paragraph>Seu cadastro como {papel} foi realizado com sucesso.</Paragraph>
    </View>
  );
}
