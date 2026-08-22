import { fireEvent, render, screen } from '@testing-library/react-native';

import { EntrarEmNovaTurmaForm } from './EntrarEmNovaTurmaForm';

describe('EntrarEmNovaTurmaForm', () => {
  it('renders only the código field — nome/contato vêm da sessão (issue #144)', async () => {
    await render(
      <EntrarEmNovaTurmaForm codigo="" enviando={false} onChangeCodigo={jest.fn()} onSubmit={jest.fn()} />,
    );

    expect(screen.getByPlaceholderText('00000')).toBeTruthy();
    expect(screen.queryByPlaceholderText('Nome')).toBeNull();
  });

  it('calls onChangeCodigo when the código field changes', async () => {
    const onChangeCodigo = jest.fn();
    await render(
      <EntrarEmNovaTurmaForm codigo="" enviando={false} onChangeCodigo={onChangeCodigo} onSubmit={jest.fn()} />,
    );

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');

    expect(onChangeCodigo).toHaveBeenCalledWith('12345');
  });

  it('calls onSubmit when Entrar na turma is pressed', async () => {
    const onSubmit = jest.fn();
    await render(
      <EntrarEmNovaTurmaForm codigo="12345" enviando={false} onChangeCodigo={jest.fn()} onSubmit={onSubmit} />,
    );

    await fireEvent.press(screen.getByText('Entrar na turma'));

    expect(onSubmit).toHaveBeenCalled();
  });

  it('shows a disabled sending state and the Api error message', async () => {
    await render(
      <EntrarEmNovaTurmaForm
        codigo="12345"
        erro="Não foi possível concluir a operação."
        enviando={true}
        onChangeCodigo={jest.fn()}
        onSubmit={jest.fn()}
      />,
    );

    expect(screen.getByText('Entrando...')).toBeTruthy();
    expect(screen.getByText('Não foi possível concluir a operação.')).toBeTruthy();
  });
});
