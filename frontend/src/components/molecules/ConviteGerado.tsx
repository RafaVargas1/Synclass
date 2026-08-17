import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type ConviteGeradoProps = {
  linkConvite: string;
  onEnviarWhatsApp: () => void;
};

/**
 * Molécula: confirmação inline de convite gerado (issue #2) — mostra o link
 * de aceite (para o Professor copiar manualmente, se preferir) e o botão de
 * enviar por WhatsApp. Não conhece `Linking` (side effect de plataforma):
 * apenas emite `onEnviarWhatsApp`, que a tela implementa.
 */
export function ConviteGerado({ linkConvite, onEnviarWhatsApp }: ConviteGeradoProps) {
  return (
    <View accessibilityRole="alert" className="w-full items-center gap-two">
      <Heading level={1}>Convite gerado!</Heading>
      <Paragraph selectable>{linkConvite}</Paragraph>
      <Button label="Enviar por WhatsApp" onPress={onEnviarWhatsApp} />
    </View>
  );
}
