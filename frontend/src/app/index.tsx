import { HomeTemplate } from '@/components/templates/HomeTemplate';

export default function HomeScreen() {
  return <HomeTemplate onGetStarted={() => console.log('Synclass: fundação pronta')} />;
}
