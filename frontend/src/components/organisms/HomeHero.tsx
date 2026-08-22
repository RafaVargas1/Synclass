import { Pressable, Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { Divisor } from '@/components/atoms/Divisor';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Marca } from '@/components/atoms/Marca';
import { Paragraph } from '@/components/atoms/Paragraph';
import {
  BotaoLoginGoogle,
  type ResultadoAutenticadoGoogle,
} from '@/components/molecules/BotaoLoginGoogle';
import { Fonts } from '@/theme/tokens';

export type HomeHeroProps = {
  onAutenticadoGoogle: (resultado: ResultadoAutenticadoGoogle) => void;
  onCadastroPendenteGoogle: (email: string) => void;
  onEntrarComoProfessor: () => void;
  onEntrarComoAluno: () => void;
  onLogin: () => void;
  /** Erro ao concluir o login Google já autenticado (ex: falha ao persistir a sessão no dispositivo). */
  erro?: string;
};

/**
 * Organismo: seção de apresentação da Home, na direção visual art deco
 * aprovada pelo Rafael (moldura escalonada em vez de sombra/gradiente —
 * ver docs/spec/design-system.md#tipografia para a decisão da fonte de
 * destaque `Fonts.deco`).
 *
 * Hierarquia de entrada (issues #111/#112/#114, achados de UX): "Entrar
 * com Google" é o CTA de maior destaque — autenticação de um toque, o
 * caminho mais frequente pra quem já tem conta, direto na Home sem
 * navegar antes pra `/login` (Hick's Law,
 * `docs/spec/ux-heuristics.md#número-de-opções-simultâneas`). "Entrar com
 * código" é o link secundário pra quem prefere OTP. Os CTAs de cadastro
 * ("Cadastrar como Professor"/"Cadastrar como Aluno", nomeados pela ação —
 * não mais "sou X", que comunicava identidade e não ação, achado de
 * reconhecimento de `docs/spec/ux-heuristics.md`) ficam por último, em
 * variante secundária — não competem em peso visual com as ações de
 * entrar, já que cadastro é o caminho menos frequente (só primeira visita).
 */
export function HomeHero({
  onAutenticadoGoogle,
  onCadastroPendenteGoogle,
  onEntrarComoProfessor,
  onEntrarComoAluno,
  onLogin,
  erro,
}: HomeHeroProps) {
  return (
    <View className="w-full max-w-[480px] items-center gap-five">
      <MolduraDaMarca />
      <Paragraph className="max-w-[420px] text-center text-base leading-6">
        Professor organiza horários e frequência dos seus Alunos, Aluno confirma presença e
        acompanha o que deve, tudo em um só lugar.
      </Paragraph>
      <View className="w-full gap-three">
        <BotaoLoginGoogle onAutenticado={onAutenticadoGoogle} onCadastroPendente={onCadastroPendenteGoogle} />
        {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
        <Pressable accessibilityRole="button" onPress={onLogin} className="items-center py-two">
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Entrar com código ou e-mail
          </Text>
        </Pressable>
      </View>
      <Divisor />
      <View className="w-full gap-three">
        <Button label="Cadastrar como Professor" variante="secundario" onPress={onEntrarComoProfessor} />
        <Button label="Cadastrar como Aluno" variante="secundario" onPress={onEntrarComoAluno} />
      </View>
    </View>
  );
}

function MolduraDaMarca() {
  return (
    <View className="items-center">
      <Marca />
      <View className="border border-text bg-background px-six py-five dark:border-dark-text dark:bg-dark-background">
        <View className="absolute inset-1.5 border border-border dark:border-dark-border" />
        <Text
          className="text-center text-5xl tracking-widest text-text dark:text-dark-text"
          style={{ fontFamily: Fonts.deco }}
        >
          SYNCLASS
        </Text>
        <Text className="mt-one text-center text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
          Agenda entre Professor e Aluno
        </Text>
      </View>
      <Marca invertido />
    </View>
  );
}
