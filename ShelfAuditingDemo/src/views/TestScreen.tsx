import React from 'react'
import { 
    View, 
    Text,
    TouchableOpacity,
    StyleSheet, 
    Alert 
} from 'react-native';
import { width, height } from '../../constants';

class TestScreen extends React.Component {
    static navigationOptions = ({ navigation }) => {
        const { params } = navigation.state;
        return { 
            title: 'Test page',
            headerRight: () => (
                <TouchableOpacity activeOpacity={0.6} onPress={() => params.testApply()}
                    style={{ padding: 4, marginRight: 10 }}>
                    <Text style={{color: 'white', fontSize: 16, fontWeight: 'bold' }}>Apply</Text>
                </TouchableOpacity>
            )
        }
    }

    constructor(props) {
        super(props);

        this.state = {
            scale: 1
        };
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

export { TestScreen };