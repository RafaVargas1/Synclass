import { Text, View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type AlunoProvisorioConfirmadoProps = {
  nome: string;
  identificador: string;
};

/**
 * Molécula: confirmação inline de cadastro de Aluno provisório concluído
 * (issue #3) — sem indicação visual de "capacidade reduzida", o Aluno
 * provisório já pode ser agendado e cobrado normalmente (Regra de Negócio
 * do card). Mostra o identificador gerado pelo sistema (issue #159) — o
 * Professor não digita mais um, precisa ver o que foi gerado pra usar como
 * referência.
 */
export function AlunoProvisorioConfirmado({ nome, identificador }: AlunoProvisorioConfirmadoProps) {
  return (
    <View accessibilityRole="alert" className="items-center gap-two">
      <Heading level={1}>Aluno provisório cadastrado!</Heading>
      <Paragraph>{nome} já pode ser agendado e cobrado normalmente.</Paragraph>
      <Paragraph>
        Identificador: <Text className="font-semibold text-text dark:text-dark-text">{identificador}</Text>
      </Paragraph>
    </View>
  );
}
