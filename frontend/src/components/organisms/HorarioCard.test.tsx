import { fireEvent, render, screen } from '@testing-library/react-native';

import { TipoMarcacao } from '@/lib/api/horarios';

import { HorarioCard } from './HorarioCard';

jest.mock('expo-router', () => {
  const React = jest.requireActual('react');
  return {
    Link: ({ href, children }: { href: string; children: React.ReactElement }) =>
      React.cloneElement(children, { accessibilityHint: href }),
  };
});

const horario = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: TipoMarcacao.Livre,
  prazoCancelamentoMinutos: 0,
};

async function renderComCard(overrides = {}) {
  return render(
    <HorarioCard
      professorId="prof-1"
      horario={horario}
      onRemover={jest.fn()}
      onAlterarPolitica={jest.fn()}
      onAlterarPrazoCancelamento={jest.fn()}
      {...overrides}
    />,
  );
}

describe('HorarioCard', () => {
  it('shows the day, start time and duration', async () => {
    await renderComCard();

    expect(screen.getByText(/Terça/)).toBeTruthy();
    expect(screen.getByText(/10:00/)).toBeTruthy();
    expect(screen.getByText(/60 min/)).toBeTruthy();
  });

  it('shows "Individual" when limiteAlunos is 1', async () => {
    await renderComCard();

    expect(screen.getByText(/Individual/)).toBeTruthy();
  });

  it('shows "Grupo até N" when limiteAlunos is greater than 1', async () => {
    const horarioEmGrupo = { ...horario, limiteAlunos: 4 };
    await renderComCard({ horario: horarioEmGrupo });

    expect(screen.getByText(/Grupo até 4/)).toBeTruthy();
  });

  it.each([
    [TipoMarcacao.Livre, 'Livre'],
    [TipoMarcacao.Fixo, 'Fixo'],
    [TipoMarcacao.Hibrido, 'Híbrido'],
  ])('shows the rótulo of tipoMarcacao %s as %s', async (tipoMarcacao, rotulo) => {
    const horarioComPolitica = { ...horario, tipoMarcacao };
    await renderComCard({ horario: horarioComPolitica });

    expect(screen.getByText(rotulo)).toBeTruthy();
  });

  it('calls onRemover with the horario id when the remove button is pressed', async () => {
    const onRemover = jest.fn();
    await renderComCard({ onRemover });

    await fireEvent.press(screen.getByText('Remover'));

    expect(onRemover).toHaveBeenCalledWith('h1');
  });

  it('links to the Chamada screen of this horario (issue #184)', async () => {
    await renderComCard();

    expect(screen.getByText('Chamada').parent).toHaveProp(
      'accessibilityHint',
      '/professor/prof-1/horarios/h1/chamada',
    );
  });

  it('gives the Remover, Chamada and Editar política buttons a touch target of at least 44x44 (Fitts/HIG)', async () => {
    await renderComCard();

    expect(screen.getByText('Remover').parent).toHaveStyle({ minWidth: 44, minHeight: 44 });
    expect(screen.getByText('Chamada').parent).toHaveStyle({ minWidth: 44, minHeight: 44 });
    expect(screen.getByText('Editar política').parent).toHaveStyle({ minWidth: 44, minHeight: 44 });
  });

  describe('modo de edição de política (issue #71)', () => {
    it('switches to edit mode with a ChipSelector when "Editar política" is pressed', async () => {
      await renderComCard();

      await fireEvent.press(screen.getByText('Editar política'));

      expect(screen.getByText('Política de marcação')).toBeTruthy();
      expect(screen.getByText('Salvar')).toBeTruthy();
      expect(screen.getByText('Cancelar')).toBeTruthy();
    });

    it('gives the Cancelar and Salvar buttons a touch target of at least 44x44 (Fitts/HIG)', async () => {
      await renderComCard();

      await fireEvent.press(screen.getByText('Editar política'));

      expect(screen.getByText('Cancelar').parent).toHaveStyle({ minWidth: 44, minHeight: 44 });
      expect(screen.getByText('Salvar').parent).toHaveStyle({ minWidth: 44, minHeight: 44 });
    });

    it('calls onAlterarPolitica with the horario id and the selected tipo when saved', async () => {
      const onAlterarPolitica = jest.fn();
      await renderComCard({ onAlterarPolitica });

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.press(screen.getByText('Fixo'));
      await fireEvent.press(screen.getByText('Salvar'));

      expect(onAlterarPolitica).toHaveBeenCalledWith('h1', TipoMarcacao.Fixo);
    });

    it('returns to normal mode on cancel without calling onAlterarPolitica', async () => {
      const onAlterarPolitica = jest.fn();
      await renderComCard({ onAlterarPolitica });

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.press(screen.getByText('Cancelar'));

      expect(onAlterarPolitica).not.toHaveBeenCalled();
      expect(screen.queryByText('Salvar')).toBeNull();
    });
  });

  describe('edição de prazo de cancelamento (issue #187)', () => {
    it('calls onAlterarPrazoCancelamento with the horario id and the typed value when saved', async () => {
      const onAlterarPrazoCancelamento = jest.fn();
      await renderComCard({ onAlterarPrazoCancelamento });

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.changeText(screen.getByPlaceholderText('0'), '90');
      await fireEvent.press(screen.getByText('Salvar'));

      expect(onAlterarPrazoCancelamento).toHaveBeenCalledWith('h1', 90);
    });

    it('does not call onAlterarPrazoCancelamento when cancelled', async () => {
      const onAlterarPrazoCancelamento = jest.fn();
      await renderComCard({ onAlterarPrazoCancelamento });

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.changeText(screen.getByPlaceholderText('0'), '90');
      await fireEvent.press(screen.getByText('Cancelar'));

      expect(onAlterarPrazoCancelamento).not.toHaveBeenCalled();
    });
  });
});
