import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    chartTitlePanel: {
        flexDirection: 'row', 
        justifyContent: 'space-between',
    },
    chartTitle: {
        color: 'white',
        opacity: 0.8,
        fontSize: 13
    },
    chartLabel: {
        color: 'white',
        fontSize: 11,
        opacity: 0.6
    },
    chartBar: {
        flexDirection: 'row', 
        height: 25,
        marginTop: 9,
        marginBottom: 12,
    },
    chartTaggedItem: {
        backgroundColor: '#248FFF', 
        alignItems: 'center', 
        justifyContent: 'center',
    },
    chartUnknownItem: {
        backgroundColor: '#B4009E',
        alignItems: 'center', 
        justifyContent: 'center',
        width: '25%',
    },
    chartShelfItem: {
        backgroundColor: '#FABE14',
        alignItems: 'center',
        justifyContent: 'center',
        width: '5%'
    },
    chartLegend: {
        flexDirection: 'row', 
        alignItems: 'center'
    },
    chartTaggedItemLegend: {
        backgroundColor: '#248FFF', 
        height: 8, 
        width: 8, 
        marginRight: 4
    },
    chartUnknownItemLegend: {
        backgroundColor: '#B4009E',
        height: 8, 
        width: 8, 
        marginRight: 4
    },
    chartShelfItemLegend: {
        backgroundColor: '#FABE14',
        height: 8, 
        width: 8, 
        marginRight: 4
    }
})
