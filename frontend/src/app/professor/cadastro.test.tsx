import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cadastrarProfessor } from '@/lib/api/professores';

import CadastroProfessorScreen from './cadastro';

jest.mock('@/lib/api/professores', () => ({
  cadastrarProfessor: jest.fn(),
}));

const cadastrarProfessorMock = cadastrarProfessor as jest.Mock;

describe('CadastroProfessorScreen', () => {
  beforeEach(() => {
    cadastrarProfessorMock.mockReset();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    cadastrarProfessorMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarProfessorMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O contato já está cadastrado como Professor.',
    });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('O contato já está cadastrado como Professor.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });
});
