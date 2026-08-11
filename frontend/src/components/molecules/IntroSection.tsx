import { View } from 'react-native';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';

export type IntroSectionProps = {
  title: string;
  description: string;
};

/**
 * Molécula: composição de dois átomos (Heading + Paragraph) com um
 * propósito único — apresentar um título e sua descrição.
 */
export function IntroSection({ title, description }: IntroSectionProps) {
  return (
    <View className="gap-two">
      <Heading level={1}>{title}</Heading>
      <Paragraph>{description}</Paragraph>
    </View>
  );
}
