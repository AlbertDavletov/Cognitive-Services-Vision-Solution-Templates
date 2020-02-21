import { createAppContainer, getActiveChildNavigationOptions } from 'react-navigation';
import { createStackNavigator } from 'react-navigation-stack';
import { 
  InputScreen, 
  ReviewScreen, 
  ResultScreen, 
  CameraScreen, 
  AddEditScreen, 
  TagCollectionScreen, 
  TestScreen 
} from './src/views';
import Icon from 'react-native-vector-icons/EvilIcons';
Icon.loadFont();

const MainNavigator = createStackNavigator({
  Input: { screen: InputScreen },
  Review: { screen: ReviewScreen },
  Result: {
    screen: ResultScreen,
    navigationOptions: { title: 'Detection results' }
  },
  AddEdit: { screen: AddEditScreen },
  TagCollection: {
    screen: TagCollectionScreen,
    navigationOptions: { title: 'Choose new label' }
  },
  Test: { screen: TestScreen },
  Camera: {
    screen: CameraScreen,
    navigationOptions: { title: 'Camera' }
  }},
  {
    initialRouteName: 'Input',
    navigationOptions: ({ navigation, screenProps }) => ({
      ...getActiveChildNavigationOptions(navigation, screenProps),
    }),
    defaultNavigationOptions: {
      headerStyle: { backgroundColor: '#1F1F1F' },
      headerTintColor: '#fff',
      headerTitleStyle: { fontWeight: 'bold' }
    }
  }
);

const App = createAppContainer(MainNavigator);

export default App;
