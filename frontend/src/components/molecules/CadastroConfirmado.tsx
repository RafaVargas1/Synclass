import { Link } from 'expo-router';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type CadastroConfirmadoProps = {
  /** Papel cadastrado (issue #61 generaliza este componente para Professor e Aluno). */
  papel: 'Professor' | 'Aluno';
};

/**
 * Molécula: confirmação inline de cadastro concluído, com CTA pra entrar
 * (issue #143) — antes ficava numa tela sem saída (o comentário original
 * dizia "não há para onde navegar ainda", desatualizado desde que o login
 * por código, issue #18, existe). Não pré-preenche o contato via `?email=`
 * em `/login`: esse parâmetro já tem outro significado lá (dispara o fluxo
 * de "cadastro pendente" em `login/index.tsx`, pra quem tentou entrar sem
 * conta) — reaproveitá-lo aqui mostraria a tela errada.
 */
export function CadastroConfirmado({ papel }: CadastroConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-four">
      <View className="items-center gap-two">
        <Heading level={1}>Cadastro concluído!</Heading>
        <Paragraph>Seu cadastro como {papel} foi realizado com sucesso.</Paragraph>
      </View>
      <Link href="/login" asChild>
        <Button label="Entrar" />
      </Link>
    </View>
  );
}
