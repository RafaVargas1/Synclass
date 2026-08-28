import { Link } from 'expo-router';
import { ClipboardText, Trash, X } from 'phosphor-react-native';
import { useState } from 'react';
import { Pressable, Text, useColorScheme, View } from 'react-native';

import { ChipSelector } from '@/components/molecules/ChipSelector';
import { FormField } from '@/components/molecules/FormField';
import { TipoMarcacao, type Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { OpcoesTipoMarcacao } from '@/lib/opcoesTipoMarcacao';
import { AlvoDeToqueMinimo, Colors } from '@/theme/tokens';

export type HorarioCardProps = {
  professorId: string;
  horario: Horario;
  onRemover: (horarioId: string) => void;
  onAlterarPolitica: (horarioId: string, tipoMarcacao: TipoMarcacao) => void;
  onAlterarPrazoCancelamento: (horarioId: string, prazoCancelamentoMinutos: number) => void;
};

const RotulosTipoMarcacao: Record<TipoMarcacao, string> = {
  [TipoMarcacao.Livre]: 'Livre',
  [TipoMarcacao.Fixo]: 'Fixo',
  [TipoMarcacao.Hibrido]: 'Híbrido',
};

/**
 * Organismo: item da lista de horários disponíveis do Professor (issue #6).
 * Mostra dia, hora de início, duração, limite de alunos e a política de
 * marcação do horário (issue #76) — sem opção de editar duração (imutável
 * após criação, ver Regra de Negócio da issue #6), só remover. Desde a issue
 * #71, a política de marcação (`TipoMarcacao`) é a única coisa editável
 * depois de criado — um botão "Editar política" alterna para um modo inline
 * com `ChipSelector` (mesmas opções de `HorarioForm`), salvando via
 * `onAlterarPolitica` ou retornando ao modo normal em `Cancelar`.
 */
export function HorarioCard({
  professorId,
  horario,
  onRemover,
  onAlterarPolitica,
  onAlterarPrazoCancelamento,
}: HorarioCardProps) {
  const [editando, setEditando] = useState(false);
  const [tipoSelecionado, setTipoSelecionado] = useState(horario.tipoMarcacao);
  const [prazoSelecionado, setPrazoSelecionado] = useState(
    String(horario.prazoCancelamentoMinutos),
  );

  function handleEditar() {
    setTipoSelecionado(horario.tipoMarcacao);
    setPrazoSelecionado(String(horario.prazoCancelamentoMinutos));
    setEditando(true);
  }

  function handleSalvar() {
    onAlterarPolitica(horario.id, tipoSelecionado);
    const prazo = Number(prazoSelecionado);
    if (Number.isInteger(prazo) && prazo >= 0) {
      onAlterarPrazoCancelamento(horario.id, prazo);
    }
    setEditando(false);
  }

  return (
    <View className="w-full rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <CardCorpo professorId={professorId} horario={horario} onRemover={onRemover} />
      {editando ? (
        <View className="mt-two w-full gap-two">
          <ChipSelector
            label="Política de marcação"
            descricao="Livre: qualquer Aluno se inscreve. Fixo: só o Professor atribui. Híbrido: Professor atribui vagas fixas e libera o restante."
            opcoes={OpcoesTipoMarcacao}
            valor={tipoSelecionado}
            onChange={setTipoSelecionado}
          />
          <FormField
            label="Prazo de cancelamento (minutos)"
            value={prazoSelecionado}
            onChangeText={setPrazoSelecionado}
            placeholder="0"
            keyboardType="numeric"
          />
          <View className="flex-row justify-end gap-two">
            <Pressable
              accessibilityRole="button"
              onPress={() => setEditando(false)}
              className="flex-row items-center justify-center gap-one"
              style={AlvoDeToqueMinimo}
            >
              <CancelarEdicaoIcone />
              <Text className="text-sm font-semibold text-text-secondary dark:text-dark-text-secondary">
                Cancelar
              </Text>
            </Pressable>
            <Pressable
              accessibilityRole="button"
              onPress={handleSalvar}
              className="items-center justify-center"
              style={AlvoDeToqueMinimo}
            >
              <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
                Salvar
              </Text>
            </Pressable>
          </View>
        </View>
      ) : (
        <Pressable
          accessibilityRole="button"
          onPress={handleEditar}
          className="mt-two items-center justify-center"
          style={AlvoDeToqueMinimo}
        >
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Editar política
          </Text>
        </Pressable>
      )}
    </View>
  );
}

/** Ícone "X" do botão "Cancelar" da edição inline (issue #202) — mesmo
 *  glifo usado em `AulaProximaCard` para a mesma ação (critério de aceite
 *  3: mesma ação, mesmo glifo em toda tela). Cor `text-secondary`, mesma
 *  do texto do botão, resolvida do tema como os demais ícones desta Task.
 */
function CancelarEdicaoIcone() {
  const escuro = useColorScheme() === 'dark';
  const paleta = escuro ? Colors.dark : Colors.light;
  return (
    // `IconProps` do phosphor-react-native não declara props de
    // acessibilidade do RN — a View em volta esconde o ícone da árvore de
    // acessibilidade, o texto "Cancelar" continua sendo o nome acessível.
    <View testID="icone-acao-cancelar" accessibilityElementsHidden importantForAccessibility="no">
      <X size={20} weight="regular" color={paleta.textSecondary} />
    </View>
  );
}

function CardCorpo({
  professorId,
  horario,
  onRemover,
}: {
  professorId: string;
  horario: Horario;
  onRemover: (id: string) => void;
}) {
  const escuro = useColorScheme() === 'dark';
  const paleta = escuro ? Colors.dark : Colors.light;
  const corPrimaria = paleta.primary;
  const corErro = paleta.error;
  const horaFormatada = horario.horaInicio.slice(0, 5);
  const rotuloLimiteAlunos =
    horario.limiteAlunos > 1 ? `Grupo até ${horario.limiteAlunos}` : 'Individual';
  const rotuloPolitica = RotulosTipoMarcacao[horario.tipoMarcacao];

  return (
    <View className="w-full flex-row items-center justify-between">
      <View>
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[horario.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {horario.duracaoMinutos} min · {rotuloLimiteAlunos}
        </Text>
        <Text
          testID="horario-politica-atual"
          className="text-sm text-text-secondary dark:text-dark-text-secondary"
        >
          {rotuloPolitica}
        </Text>
      </View>
      <View className="flex-row items-center gap-two">
        <Link href={`/professor/${professorId}/horarios/${horario.id}/chamada`} asChild>
          <Pressable
            accessibilityRole="button"
            className="flex-row items-center justify-center gap-one"
            style={AlvoDeToqueMinimo}
          >
            <View testID="icone-acao-chamada" accessibilityElementsHidden importantForAccessibility="no">
              <ClipboardText size={20} weight="regular" color={corPrimaria} />
            </View>
            <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
              Chamada
            </Text>
          </Pressable>
        </Link>
        <Pressable
          accessibilityRole="button"
          onPress={() => onRemover(horario.id)}
          className="flex-row items-center justify-center gap-one"
          style={AlvoDeToqueMinimo}
        >
          <View testID="icone-acao-remover" accessibilityElementsHidden importantForAccessibility="no">
            <Trash size={20} weight="regular" color={corErro} />
          </View>
          <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
        </Pressable>
      </View>
    </View>
  );
}
