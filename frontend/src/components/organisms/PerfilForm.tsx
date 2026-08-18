import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { FormField } from '@/components/molecules/FormField';

export type PerfilFormProps = {
  nome: string;
  erro?: string;
  sucesso: boolean;
  salvando: boolean;
  onChangeNome: (nome: string) => void;
  onSalvar: () => void;
};

/**
 * Organismo: formulário de edição do próprio nome (issue #27). Mesmo
 * formato de `CadastroProfessorForm` — não conhece a Api, só emite
 * callbacks para a tela controlar o fluxo de salvar.
 */
export function PerfilForm({ nome, erro, sucesso, salvando, onChangeNome, onSalvar }: PerfilFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField label="Nome" value={nome} onChangeText={onChangeNome} placeholder="Seu nome completo" />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      {sucesso ? <Paragraph accessibilityRole="alert">Nome atualizado com sucesso.</Paragraph> : null}
      <Button label={salvando ? 'Salvando...' : 'Salvar'} onPress={onSalvar} disabled={salvando} />
    </View>
  );
}
