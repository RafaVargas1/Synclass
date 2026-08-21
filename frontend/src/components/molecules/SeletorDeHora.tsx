import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

const Horas = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const Minutos = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));

export type SeletorDeHoraProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (hora: string) => void;
};

/**
 * Sem valor prévio, hora/minuto começam indefinidos (não "00:00") — achado
 * de dev-review, PR #120: pré-selecionar 00:00 permitia confirmar sem o
 * usuário escolher nada, enviando um horário não intencional.
 */
function desmembrarValor(valor: string | undefined): [string | undefined, string | undefined] {
  if (!valor) {
    return [undefined, undefined];
  }
  const [hora, minuto] = valor.split(':');
  return [hora, minuto];
}

/**
 * Molécula: campo de hora que abre um painel com duas listas roláveis
 * horizontais de números (Hora 00-23 e Minuto 00-59) em vez de aceitar
 * `HH:mm` digitado à mão (issue #117 — campo de hora sem máscara/seletor).
 * Mantém o range completo 00:00-23:59 e fecha ao confirmar a escolha — ou
 * ao tocar o campo de novo com o painel já aberto (mesmo toggle de
 * `SeletorDeData`), sem alterar o valor.
 */
export function SeletorDeHora({ label, valor, onSelecionar }: SeletorDeHoraProps) {
  const [aberto, setAberto] = useState(false);
  const [horaSelecionada, setHoraSelecionada] = useState<string | undefined>(
    () => desmembrarValor(valor)[0],
  );
  const [minutoSelecionado, setMinutoSelecionado] = useState<string | undefined>(
    () => desmembrarValor(valor)[1],
  );

  function alternar() {
    if (!aberto) {
      const [hora, minuto] = desmembrarValor(valor);
      setHoraSelecionada(hora);
      setMinutoSelecionado(minuto);
    }
    setAberto((atual) => !atual);
  }

  function confirmar() {
    if (!horaSelecionada || !minutoSelecionado) {
      return;
    }
    onSelecionar(`${horaSelecionada}:${minutoSelecionado}`);
    setAberto(false);
  }

  const podeConfirmar = Boolean(horaSelecionada && minutoSelecionado);

  return (
    <View className="gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Pressable
        accessibilityRole="button"
        onPress={alternar}
        className="border border-border bg-background-element px-three py-two dark:border-dark-border dark:bg-dark-background-element"
      >
        <Text className="text-sm text-text dark:text-dark-text">
          {valor ? valor : 'Selecionar hora'}
        </Text>
      </Pressable>
      {aberto ? (
        <View className="gap-one">
          <ListaDeNumeros
            titulo="Hora"
            prefixoRotulo="Hora"
            valores={Horas}
            selecionado={horaSelecionada}
            onSelecionar={setHoraSelecionada}
          />
          <ListaDeNumeros
            titulo="Minuto"
            prefixoRotulo="Minuto"
            valores={Minutos}
            selecionado={minutoSelecionado}
            onSelecionar={setMinutoSelecionado}
          />
          <Pressable
            accessibilityRole="button"
            onPress={confirmar}
            disabled={!podeConfirmar}
            className={`self-start border border-primary bg-primary px-three py-two dark:border-dark-primary dark:bg-dark-primary ${
              podeConfirmar ? '' : 'opacity-40'
            }`}
          >
            <Text className="font-medium text-white">Confirmar</Text>
          </Pressable>
        </View>
      ) : null}
    </View>
  );
}

type ListaDeNumerosProps = {
  titulo: string;
  prefixoRotulo: string;
  valores: readonly string[];
  selecionado: string | undefined;
  onSelecionar: (valor: string) => void;
};

/**
 * Lista rolável horizontal de números (Hora ou Minuto) — extraída pra não
 * duplicar o mesmo `ScrollView`/`Pressable` entre as duas listas do
 * `SeletorDeHora` (achado de dev-review, PR #120: função original passava
 * do limite de 4-20 linhas por repetir o mesmo bloco duas vezes).
 */
function ListaDeNumeros({ titulo, prefixoRotulo, valores, selecionado, onSelecionar }: ListaDeNumerosProps) {
  return (
    <>
      <Text className="text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
        {titulo}
      </Text>
      <ScrollView horizontal>
        <View className="flex-row">
          {valores.map((valor) => (
            <Pressable
              key={valor}
              accessibilityRole="button"
              accessibilityLabel={`${prefixoRotulo} ${valor}`}
              accessibilityState={{ selected: valor === selecionado }}
              onPress={() => onSelecionar(valor)}
              className="items-center justify-center"
              style={{ minWidth: 44, minHeight: 44 }}
            >
              <Text
                className={
                  valor === selecionado
                    ? 'font-bold text-primary dark:text-dark-primary'
                    : 'text-sm text-text dark:text-dark-text'
                }
              >
                {valor}
              </Text>
            </Pressable>
          ))}
        </View>
      </ScrollView>
    </>
  );
}
