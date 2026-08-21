import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { ChipSelector } from '@/components/molecules/ChipSelector';
import { TipoMarcacao, type Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { OpcoesTipoMarcacao } from '@/lib/opcoesTipoMarcacao';

export type HorarioCardProps = {
  horario: Horario;
  onRemover: (horarioId: string) => void;
  onAlterarPolitica: (horarioId: string, tipoMarcacao: TipoMarcacao) => void;
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
export function HorarioCard({ horario, onRemover, onAlterarPolitica }: HorarioCardProps) {
  const [editando, setEditando] = useState(false);
  const [tipoSelecionado, setTipoSelecionado] = useState(horario.tipoMarcacao);

  function handleEditar() {
    setTipoSelecionado(horario.tipoMarcacao);
    setEditando(true);
  }

  function handleSalvar() {
    onAlterarPolitica(horario.id, tipoSelecionado);
    setEditando(false);
  }

  return (
    <View className="w-full rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <CardCorpo horario={horario} onRemover={onRemover} />
      {editando ? (
        <View className="mt-two w-full gap-two">
          <ChipSelector
            label="Política de marcação"
            opcoes={OpcoesTipoMarcacao}
            valor={tipoSelecionado}
            onChange={setTipoSelecionado}
          />
          <View className="flex-row justify-end gap-two">
            <Pressable accessibilityRole="button" onPress={() => setEditando(false)}>
              <Text className="text-sm font-semibold text-text-secondary dark:text-dark-text-secondary">
                Cancelar
              </Text>
            </Pressable>
            <Pressable accessibilityRole="button" onPress={handleSalvar}>
              <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
                Salvar
              </Text>
            </Pressable>
          </View>
        </View>
      ) : (
        <Pressable accessibilityRole="button" onPress={handleEditar} className="mt-two">
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Editar política
          </Text>
        </Pressable>
      )}
    </View>
  );
}

function CardCorpo({ horario, onRemover }: { horario: Horario; onRemover: (id: string) => void }) {
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
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {rotuloPolitica}
        </Text>
      </View>
      <Pressable accessibilityRole="button" onPress={() => onRemover(horario.id)}>
        <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
      </Pressable>
    </View>
  );
}
