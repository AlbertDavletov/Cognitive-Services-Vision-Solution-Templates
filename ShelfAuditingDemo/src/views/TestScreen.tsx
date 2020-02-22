import React from 'react'
import { 
    View, 
    Text,
    TouchableOpacity,
    StyleSheet, 
    Alert 
} from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { width, height } from '../../constants'

interface TestScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

export class TestScreen extends React.Component<TestScreenProps, {}> {
    static navigationOptions = ({ navigation } : { navigation : NavigationScreenProp<NavigationState,NavigationParams> }) => {
        const { params } = navigation.state;
        return { 
            title: 'Test page',
            headerRight: () => (
                <TouchableOpacity activeOpacity={0.6} onPress={() => {
                    if (params?.testApply) {
                        params.testApply();
                    }
                }}
                    style={{ padding: 4, marginRight: 10 }}>
                    <Text style={{color: 'white', fontSize: 16, fontWeight: 'bold' }}>Apply</Text>
                </TouchableOpacity>
            )
        }
    }

    constructor(props: TestScreenProps) {
        super(props);
    }

    componentDidMount() {
        const { navigation } = this.props;
        navigation.setParams({ testApply: this.testApply });
    }

    render() {
        const { mainContainer } = this.styles;

        return (
            <View style={mainContainer}>
                <Text>Test page</Text>
            </View>
        );
    }

    testApply() {
        Alert.alert('Apply - test!')
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
        },
    })
}
