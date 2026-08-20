import { fireEvent, render, screen } from '@testing-library/react-native';

import { ConviteGerado } from './ConviteGerado';

describe('ConviteGerado', () => {
  it('renders the link and the 5-digit code (issue #62), and calls onEnviarWhatsApp when pressed', async () => {
    const onEnviarWhatsApp = jest.fn();
    await render(
      <ConviteGerado
        linkConvite="https://synclass.app/convite/token-1"
        codigo="12345"
        onEnviarWhatsApp={onEnviarWhatsApp}
      />,
    );

    expect(screen.getByText('Convite gerado!')).toBeTruthy();
    expect(screen.getByText(/https:\/\/synclass\.app\/convite\/token-1/)).toBeTruthy();
    expect(screen.getByText(/12345/)).toBeTruthy();

    await fireEvent.press(screen.getByText('Enviar por WhatsApp'));

    expect(onEnviarWhatsApp).toHaveBeenCalledTimes(1);
  });
});
