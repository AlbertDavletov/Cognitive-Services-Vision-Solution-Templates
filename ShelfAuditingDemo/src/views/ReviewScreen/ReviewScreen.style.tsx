import { StyleSheet } from 'react-native'

export const ButtonActiveColor = 'white';
export const ButtonDisabledColor = 'gray';

export const styles = StyleSheet.create({
    mainContainer: {
        flex: 1,
        backgroundColor: 'black'
    },
    loading: {
        position: 'absolute',
        left: 0,
        right: 0,
        top: 0,
        bottom: 0,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'black',
        opacity: 0.7
    },
    actionBar: {
        flexDirection: 'row',   
        backgroundColor: '#1F1F1F',
        height: 49, 
        justifyContent: 'space-around'                    
    },
    iconButton: {
        flex: 1
    }
})