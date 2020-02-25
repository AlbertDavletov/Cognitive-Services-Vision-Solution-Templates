import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    container: {
        flex: 1, 
        alignItems: 'stretch',
        backgroundColor: 'black'
    },
    centerContainer: {
        alignItems: 'center'           
    },
    horizontalContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: 14,
        height: 54
    },
    h1: {
        color: 'white',
        fontSize: 17,
        padding: 20
    },
    h2: {
        color: 'white',
        fontSize: 15,
        padding: 10
    },
    h3: {
        color: '#0A84FF',
        fontSize: 13,
        fontWeight: 'bold'
    },
    picker: {
        flex: 1,
        alignItems: 'stretch',
        color: 'white',
        marginLeft: 10, 
        marginRight: 10
    },
    line: {
        borderBottomColor: 'white',
        opacity: 0.2,
        borderBottomWidth: 1,
    },
    imageThumbnail: {
        justifyContent: 'center',
        alignItems: 'center',
        height: 200
    }
})