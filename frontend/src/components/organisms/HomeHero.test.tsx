import { fireEvent, render, screen } from '@testing-library/react-native';

import { HomeHero } from './HomeHero';

describe('HomeHero', () => {
  it('calls onGetStarted when the cadastro button is pressed', async () => {
    const onGetStarted = jest.fn();
    await render(<HomeHero onGetStarted={onGetStarted} onLogin={jest.fn()} />);

    await fireEvent.press(screen.getByText('Cadastrar como Professor'));

    expect(onGetStarted).toHaveBeenCalled();
  });

  it('calls onLogin when the entrar link is pressed', async () => {
    const onLogin = jest.fn();
    await render(<HomeHero onGetStarted={jest.fn()} onLogin={onLogin} />);

    await fireEvent.press(screen.getByText('Já tenho conta — Entrar'));

    expect(onLogin).toHaveBeenCalled();
  });
});
